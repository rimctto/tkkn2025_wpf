using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace tkkn2025.UI.Windows
{
    /// <summary>
    /// Debug window that captures and displays Debug.WriteLine output
    /// </summary>
    public partial class DebugWindow : Window
    {
        private StringBuilder debugOutput;
        private int lineCount = 0;
        private static DebugWindow? currentInstance;

        public DebugWindow()
        {
            InitializeComponent();
            InitializeDebugCapture();
            
            // Set initial focus to the text box for easier keyboard navigation
            Loaded += (s, e) => DebugTextBox.Focus();
        }

        /// <summary>
        /// Initialize debug output capture
        /// </summary>
        private void InitializeDebugCapture()
        {
            debugOutput = new StringBuilder();
            currentInstance = this;
            
            UpdateStatus("Debug capture initialized");
        }

        /// <summary>
        /// Static method to write debug output to the current debug window instance
        /// </summary>
        /// <param name="text">Text to write</param>
        public static void WriteDebugOutput(string text)
        {
            currentInstance?.AppendDebugText(text);
        }

        /// <summary>
        /// Append text to the debug output
        /// </summary>
        /// <param name="text">Text to append</param>
        public void AppendDebugText(string text)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendDebugText(text));
                return;
            }

            // Add timestamp to each line
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var formattedText = $"[{timestamp}] {text}";
            
            debugOutput.AppendLine(formattedText);
            DebugTextBox.AppendText(formattedText + Environment.NewLine);
            
            // Auto-scroll to bottom
            DebugTextBox.ScrollToEnd();
            
            // Update line count
            lineCount++;
            UpdateLineCount();
            
            // Update status
            UpdateStatus($"Last update: {timestamp}");
        }

        /// <summary>
        /// Update the line count display
        /// </summary>
        private void UpdateLineCount()
        {
            LineCountText.Text = $"Lines: {lineCount}";
        }

        /// <summary>
        /// Update the status text
        /// </summary>
        /// <param name="status">Status message</param>
        private void UpdateStatus(string status)
        {
            StatusText.Text = status;
        }

        /// <summary>
        /// Copy all debug output to clipboard
        /// </summary>
        private void CopyAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (debugOutput.Length > 0)
                {
                    Clipboard.SetText(debugOutput.ToString());
                    UpdateStatus("All debug output copied to clipboard");
                }
                else
                {
                    UpdateStatus("No debug output to copy");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to copy: {ex.Message}");
            }
        }

        /// <summary>
        /// Copy selected text to clipboard
        /// </summary>
        private void CopySelectedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(DebugTextBox.SelectedText))
                {
                    Clipboard.SetText(DebugTextBox.SelectedText);
                    UpdateStatus("Selected text copied to clipboard");
                }
                else
                {
                    UpdateStatus("No text selected");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to copy selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all debug output
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to clear all debug output?",
                    "Clear Debug Log",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    debugOutput.Clear();
                    DebugTextBox.Clear();
                    lineCount = 0;
                    UpdateLineCount();
                    UpdateStatus("Debug output cleared");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to clear: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up when window is closing
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Clear the current instance reference
                if (currentInstance == this)
                {
                    currentInstance = null;
                }
            }
            catch (Exception ex)
            {
                // Don't throw exceptions during cleanup
                System.Diagnostics.Debug.WriteLine($"Error during DebugWindow cleanup: {ex.Message}");
            }
            
            base.OnClosed(e);
        }
    }
}