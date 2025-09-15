using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Text.Json;

namespace tkkn2025.UI
{
    /// <summary>
    /// Interaction logic for GameConfigScreen.xaml
    /// </summary>
    public partial class GameConfigScreen : UserControl
    {
        public event EventHandler? BackRequested;
        public event EventHandler<GameConfig>? ConfigSaved;

        private GameConfig currentConfig;

        public GameConfigScreen()
        {
            InitializeComponent();
            UpdateDateCreatedText();
        }

        /// <summary>
        /// Initialize the screen with the current game configuration
        /// </summary>
        /// <param name="config">Current game configuration to save</param>
        public void Initialize(GameConfig config)
        {
            currentConfig = config;
            
            // Set default values
            ConfigNameTextBox.Text = "My Custom Config";
            DescriptionTextBox.Text = "Enter a description for this configuration...";
            UpdateDateCreatedText();
            
            // Focus on the config name textbox
            ConfigNameTextBox.Focus();
            ConfigNameTextBox.SelectAll();
        }

        private void UpdateDateCreatedText()
        {
            DateCreatedText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(ConfigNameTextBox.Text))
                {
                    MessageBox.Show("Please enter a configuration name.", "Validation Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfigNameTextBox.Focus();
                    return;
                }

                // Create updated config with metadata
                var configToSave = currentConfig ?? new GameConfig();
                configToSave.ConfigName = ConfigNameTextBox.Text.Trim();
                configToSave.Description = DescriptionTextBox.Text.Trim();
                configToSave.DateCreated = DateTime.Now;

                // Show save file dialog
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Game Configuration",
                    Filter = "Game Configuration (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = GenerateFileName(configToSave.ConfigName),
                    InitialDirectory = GetConfigDirectory()
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Save the configuration
                    bool success = SaveConfigToFile(configToSave, saveFileDialog.FileName);
                    
                    if (success)
                    {
                        MessageBox.Show($"Configuration '{configToSave.ConfigName}' saved successfully!", 
                                      "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Notify that config was saved
                        ConfigSaved?.Invoke(this, configToSave);
                        
                        // Go back to previous screen
                        BackRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        MessageBox.Show("Failed to save configuration. Please try again.", 
                                      "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error in SaveConfigButton_Click: {ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private string GenerateFileName(string configName)
        {
            // Remove invalid characters and create a safe filename
            string safeFileName = string.Join("_", configName.Split(System.IO.Path.GetInvalidFileNameChars()));
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{safeFileName}_{timestamp}.json";
        }

        private string GetConfigDirectory()
        {
            string configDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                "Saved", "GameConfigurations");
            
            try
            {
                if (!System.IO.Directory.Exists(configDir))
                {
                    System.IO.Directory.CreateDirectory(configDir);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create config directory: {ex.Message}");
                // Fallback to application directory
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
            }
            
            return configDir;
        }

        private bool SaveConfigToFile(GameConfig config, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Create a configuration object with metadata
                var configWithMetadata = new
                {
                    SavedAt = DateTime.Now,
                    Version = "1.0",
                    Configuration = config
                };

                string jsonString = JsonSerializer.Serialize(configWithMetadata, options);
                System.IO.File.WriteAllText(filePath, jsonString);

                System.Diagnostics.Debug.WriteLine($"Configuration saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save configuration to {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handle keyboard shortcuts
        /// </summary>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                BackRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
    }
}