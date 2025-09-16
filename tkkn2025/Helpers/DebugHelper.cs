using System.Diagnostics;
using tkkn2025.UI.Windows;

namespace tkkn2025.Helpers
{
    /// <summary>
    /// Helper class to intercept Debug.WriteLine calls and forward them to the DebugWindow
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// Write a line to both the system debug output and the debug window
        /// </summary>
        /// <param name="message">Message to write</param>
        public static void WriteLine(string message)
        {
            // Write to system debug output (normal behavior)
            Debug.WriteLine(message);
            
            // Also write to debug window if it exists
            DebugWindow.WriteDebugOutput(message);
        }

        /// <summary>
        /// Write a formatted line to both the system debug output and the debug window
        /// </summary>
        /// <param name="format">Format string</param>
        /// <param name="args">Arguments</param>
        public static void WriteLine(string format, params object[] args)
        {
            var message = string.Format(format, args);
            WriteLine(message);
        }
    }
}