using System.IO;
using System.Text.Json;

namespace BlogApp.Services
{
    public class ThemeService
    {
        private readonly string _filePath = "themesettings.json";
        private string _primaryColor = "#4f46e5"; // Default color

        public ThemeService()
        {
            LoadSettings();
        }

        public string GetPrimaryColor()
        {
            return _primaryColor;
        }

        public void SetPrimaryColor(string color)
        {
            _primaryColor = color;
            SaveSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var settings = JsonSerializer.Deserialize<ThemeSettingsModel>(json);
                    if (settings != null && !string.IsNullOrEmpty(settings.PrimaryColor))
                    {
                        _primaryColor = settings.PrimaryColor;
                    }
                }
                catch
                {
                    // Ignore loading errors and use default
                }
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new ThemeSettingsModel { PrimaryColor = _primaryColor };
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Ignore saving errors
            }
        }

        private class ThemeSettingsModel
        {
            public string PrimaryColor { get; set; } = string.Empty;
        }
    }
}
