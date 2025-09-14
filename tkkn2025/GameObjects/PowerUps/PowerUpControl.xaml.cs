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

namespace tkkn2025.GameObjects
{
    /// <summary>
    /// Interaction logic for PowerUp.xaml
    /// </summary>
    public partial class PowerUpControl : UserControl
    {
        public PowerUpControl()
        {
            InitializeComponent();
            PowerUpSphere.Fill = SphereColor; // set default
        }

        public static readonly DependencyProperty SphereColorProperty =
            DependencyProperty.Register(nameof(SphereColor),
                typeof(Brush),
                typeof(PowerUpControl),
                new PropertyMetadata(Brushes.LimeGreen, OnSphereColorChanged));

        public Brush SphereColor
        {
            get => (Brush)GetValue(SphereColorProperty);
            set => SetValue(SphereColorProperty, value);
        }

        private static void OnSphereColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PowerUpControl control && e.NewValue is Brush brush)
            {
                control.PowerUpSphere.Fill = brush;
            }
        }
    }
}
