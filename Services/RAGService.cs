using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BlogApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BlogApp.Services
{
    public class RAGService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        // Cache embeddings in memory so we don't spam the API for every chat
        private static readonly Dictionary<int, float[]> _postEmbeddingsCache = new Dictionary<int, float[]>();

        public RAGService(ApplicationDbContext context, IConfiguration config, HttpClient httpClient)
        {
            _context = context;
            _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
            _httpClient = httpClient;
        }

        public async Task<string> ChatAsync(List<BlogApp.Controllers.BotController.ChatMessageDto> history)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "Gemini API Key is missing. Please configure it in appsettings.json.";

            try
            {
                var userMessage = history.LastOrDefault()?.Text ?? string.Empty;
                var searchContext = string.Join(" ", history.Where(m => m.Role == "user").TakeLast(2).Select(m => m.Text));
                
                // 1. Get embedding for the user's question
                var questionEmbedding = await GetEmbeddingAsync(searchContext);
                if (questionEmbedding == null) return "Failed to understand the question (Embedding failed).";

                // 2. Fetch all published posts
                var posts = await _context.Posts.Include(p => p.Category).Where(p => p.IsApproved).ToListAsync();
                if (!posts.Any()) return "There are no approved blog posts to answer from yet.";

                // 3. Ensure all posts have embeddings cached
                foreach (var post in posts)
                {
                    if (!_postEmbeddingsCache.ContainsKey(post.Id))
                    {
                        var textToEmbed = $"Title: {post.Title}\nContent: {post.Content}";
                        var emb = await GetEmbeddingAsync(textToEmbed);
                        if (emb != null)
                        {
                            _postEmbeddingsCache[post.Id] = emb;
                        }
                    }
                }

                // 4. Calculate cosine similarity to find top 3 relevant posts
                var scoredPosts = posts
                    .Where(p => _postEmbeddingsCache.ContainsKey(p.Id))
                    .Select(p => new
                    {
                        Post = p,
                        Score = CosineSimilarity(questionEmbedding, _postEmbeddingsCache[p.Id])
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(3)
                    .ToList();

                // 5. Build context from top posts
                var contextBuilder = new System.Text.StringBuilder();
                foreach (var match in scoredPosts)
                {
                    contextBuilder.AppendLine($"--- BLOG POST ---");
                    contextBuilder.AppendLine($"Title: {match.Post.Title}");
                    contextBuilder.AppendLine($"Category: {match.Post.Category?.Name ?? "Uncategorized"}");
                    contextBuilder.AppendLine($"Link: /Posts/Details/{match.Post.Id}");
                    contextBuilder.AppendLine($"Content: {match.Post.Content}");
                    contextBuilder.AppendLine();
                }

                // 6. Call Gemini to generate an answer
                return await GenerateAnswerAsync(history, contextBuilder.ToString());
            }
            catch (Exception ex)
            {
                return $"Oops, something went wrong: {ex.Message}";
            }
        }

        private async Task<float[]?> GetEmbeddingAsync(string text)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-2:embedContent?key={_apiKey}";
            var payload = new
            {
                model = "models/gemini-embedding-2",
                content = new { parts = new[] { new { text = text } } }
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"GEMINI ERROR: {response.StatusCode} - {errorBody}");
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            try
            {
                var values = json.GetProperty("embedding").GetProperty("values").EnumerateArray();
                return values.Select(v => v.GetSingle()).ToArray();
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> GenerateAnswerAsync(List<BlogApp.Controllers.BotController.ChatMessageDto> history, string context)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            
            string systemPrompt = "You are a helpful AI assistant for a blog website. " +
                "Answer the user's question STRICTLY based on the provided BLOG POST excerpts below. " +
                "If the answer cannot be found in the provided excerpts, say 'I'm sorry, but I couldn't find an answer to that in the blog posts.' " +
                "Keep your answers concise, friendly, and formatted in plain text or markdown. " +
                "If the user asks for a link, provide the Link property from the excerpts.\n\n" +
                "CONTEXT:\n" + context;

            var contentsArray = history.Select(m => new
            {
                role = m.Role == "bot" ? "model" : "user",
                parts = new[] { new { text = m.Text } }
            }).ToArray();

            var payload = new
            {
                system_instruction = new { parts = new { text = systemPrompt } },
                contents = contentsArray
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Error generating response from Gemini API. Status: {response.StatusCode}";
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            try
            {
                var text = json.GetProperty("candidates")[0]
                               .GetProperty("content")
                               .GetProperty("parts")[0]
                               .GetProperty("text").GetString();
                return text ?? "No answer generated.";
            }
            catch
            {
                return "Failed to parse Gemini response.";
            }
        }

        private float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length) return 0f;

            float dotProduct = 0f;
            float normA = 0f;
            float normB = 0f;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            if (normA == 0 || normB == 0) return 0;
            return (float)(dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }
    }
}
