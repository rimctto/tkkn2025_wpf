using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace tkkn2025.GameObjects.Ship
{
    /// <summary>
    /// Interaction logic for ShipSprite.xaml
    /// </summary>
    public partial class ShipSprite : UserControl
    {
        public ShipSprite()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Show the left tilted ship appearance
        /// </summary>
        public void ShowLeftTilt()
        {
            ShipNeutral.Visibility = Visibility.Collapsed;
            ShipRight.Visibility = Visibility.Collapsed;
            ShipLeft.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Show the right tilted ship appearance
        /// </summary>
        public void ShowRightTilt()
        {
            ShipNeutral.Visibility = Visibility.Collapsed;
            ShipLeft.Visibility = Visibility.Collapsed;
            ShipRight.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Show the neutral ship appearance
        /// </summary>
        public void ShowNeutral()
        {
            ShipLeft.Visibility = Visibility.Collapsed;
            ShipRight.Visibility = Visibility.Collapsed;
            ShipNeutral.Visibility = Visibility.Visible;
        }
    }
}