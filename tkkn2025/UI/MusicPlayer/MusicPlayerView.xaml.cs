using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using tkkn2025.UI.MusicPlayer;

namespace tkkn2025.UI.MusicPlayer
{
    /// <summary>
    /// Interaction logic for MusicPlayerView.xaml
    /// </summary>
    public partial class MusicPlayerView : UserControl
    {
        public MusicPlayerViewModel ViewModel { get; }

        public MusicPlayerView()
        {
            InitializeComponent();
            
            ViewModel = new MusicPlayerViewModel();
            DataContext = ViewModel;
            
            // Prevent control from taking focus during game
            Focusable = false;
            
            // Handle unloaded event for cleanup
            Unloaded += MusicPlayerView_Unloaded;
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MusicEnabled = !ViewModel.MusicEnabled;
            
            // Store the currently focused element
            var focusedElement = FocusManager.GetFocusedElement(Application.Current.MainWindow);
            
            // Ensure focus doesn't get stuck on the button
            if (focusedElement is UIElement element && element.IsEnabled && element.Focusable)
            {
                element.Focus();
            }
            else
            {
                // Return focus to main window or game canvas
                Application.Current.MainWindow?.Focus();
            }
        }

        private void DefaultTrackCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.IsChecked == true)
            {
                ViewModel.SetCurrentAsDefault();
            }
            
            // Ensure focus doesn't get stuck on the checkbox
            var focusedElement = FocusManager.GetFocusedElement(Application.Current.MainWindow);
            if (focusedElement is UIElement element && element.IsEnabled && element.Focusable)
            {
                element.Focus();
            }
            else
            {
                Application.Current.MainWindow?.Focus();
            }
        }

        private void MusicPlayerView_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Dispose();
        }
    }

    /// <summary>
    /// Converter to display music icon based on enabled state
    /// </summary>
    public class BoolToMusicIconConverter : IValueConverter
    {
        public static readonly BoolToMusicIconConverter Instance = new BoolToMusicIconConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "🎵" : "🔇";
            }
            return "🔇";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to display play/pause icon based on playing state
    /// </summary>
    public class BoolToPlayPauseConverter : IValueConverter
    {
        public static readonly BoolToPlayPauseConverter Instance = new BoolToPlayPauseConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
            {
                return isPlaying ? "⏸️" : "▶️";
            }
            return "▶️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
