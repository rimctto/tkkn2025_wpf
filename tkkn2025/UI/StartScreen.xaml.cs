using System.Windows.Controls;
using System.Windows.Input;
using tkkn2025.GameObjects.PowerUps;

namespace tkkn2025.UI
{
    /// <summary>
    /// Interaction logic for StartScreen.xaml
    /// </summary>
    public partial class StartScreen : UserControl
    {
        public StartScreen()
        {
            InitializeComponent();
            InitializePowerUpVisuals();
        }

        /// <summary>
        /// Initialize the power-up visual controls with their appropriate colors
        /// </summary>
        private void InitializePowerUpVisuals()
        {
            // Set the colors to match the PowerUpManager brushes
            TimeWarpVisual.SphereColor = PowerUpManager.TimeWarpBrush;
            SingularityVisual.SphereColor = PowerUpManager.SingularityBrush;
            RepulsorVisual.SphereColor = PowerUpManager.RepulsorBrush;
        }

        /// <summary>
        /// Handle mouse clicks on the start screen to focus the game canvas
        /// </summary>
        private void StartScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Find the parent window
            var window = System.Windows.Window.GetWindow(this);
            if (window is MainWindow mainWindow)
            {
                // Focus the game canvas when the start screen is clicked
                mainWindow.GameCanvas.Focus();
            }
            
            e.Handled = true;
        }
    }
}