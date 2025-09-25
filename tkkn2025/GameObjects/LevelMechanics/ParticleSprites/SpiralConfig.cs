using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace tkkn2025.GameObjects.LevelMechanics.ParticleSprites;
public class SpiralConfig
{
    public double Radius { get; set; } = 75;
    public double ParticleSpacing { get; set; } = 0.001;
    public int Width { get; set; } = 3;
    public int Height { get; set; } = 3;
    public int ParticleCount { get; set; } = 25;
    public double Frequency { get; set; } = 0.01;
    public double Amplitude { get; set; } = 25;
    public double Speed { get; set; } = 0.03;

    private static readonly string ConfigFilePath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
        "spiral_config.json");

    public static SpiralConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<SpiralConfig>(json);
                return config ?? new SpiralConfig();
            }
        }
        catch (Exception ex)
        {
            // If loading fails, return default config
            System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
        }

        return new SpiralConfig();
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
            System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
        }
    }
}

