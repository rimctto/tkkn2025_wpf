using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace tkkn2025.GameObjects.LevelMechanics.ParticleSprites
{
    public class SineWaveConfig
    {
        public double ParticleSpacing { get; set; } = 5;
        public int Size { get; set; } = 5;
        public int ParticleCount { get; set; } = 15;
        public double Frequency { get; set; } = 0.5;
        public double Amplitude { get; set; } = 35;
        public double RotationSpeed { get; set; } = 0.003;

        private static readonly string ConfigFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
            "sinewave_config.json");

        public static SineWaveConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<SineWaveConfig>(json);
                    return config ?? new SineWaveConfig();
                }
            }
            catch (Exception ex)
            {
                // If loading fails, return default config
                System.Diagnostics.Debug.WriteLine($"Failed to load sine wave config: {ex.Message}");
            }

            return new SineWaveConfig();
        }

        public void SaveConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Failed to save sine wave config: {ex.Message}");
            }
        }
    }
}