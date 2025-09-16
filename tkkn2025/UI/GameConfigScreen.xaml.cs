using System.Windows;
using System.Windows.Controls;

namespace tkkn2025.UI
{
    /// <summary>
    /// Interaction logic for GameConfigScreen.xaml
    /// </summary>
    public partial class GameConfigScreen : UserControl
    {
        private GameConfig currentConfig = null!;

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
            currentConfig = config.CreateCopy(); // Create a copy to avoid modifying the original
            
            // Set default values for new config
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

                // Update config metadata with user input
                currentConfig.ConfigName = ConfigNameTextBox.Text.Trim();
                currentConfig.Description = DescriptionTextBox.Text.Trim();
                currentConfig.CreatedBy = Session.PlayerName;
                currentConfig.DateCreated = DateTime.Now;
                currentConfig.LastModified = DateTime.Now;
                currentConfig.Version = "2.0";

                // Use ConfigManager to save to GameSettings directory
                bool success = ConfigManager.SaveGameConfigToSettings(currentConfig);
                
                if (success)
                {
                    MessageBox.Show($"Configuration '{currentConfig.ConfigName}' saved successfully!", 
                                  "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Notify via GameEvents that config was saved
                    GameEvents.RaiseConfigurationSaved();
                    
                    // Go back to previous screen via GameEvents
                    GameEvents.RaiseHideConfigScreen();
                }
                else
                {
                    MessageBox.Show("Failed to save configuration. Please try again.", 
                                  "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // Use GameEvents to request going back
            GameEvents.RaiseHideConfigScreen();
        }
    }
}