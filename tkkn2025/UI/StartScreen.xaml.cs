using System.Windows.Controls;
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
    }
}