using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace tkkn2025
{
    public class AudioManager
    {
        private MediaPlayer music = new MediaPlayer();
        private bool isInitialized = false;

        public void Initialize()
        {
            try
            {
                // Debug: Check file existence and paths
                DebugAudioPaths();
                
                // Try different URI formats
                Uri audioUri = null;
                
                // Method 1: Try relative path (since file is copied to output)
                var executableDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
                var relativePath = System.IO.Path.Combine(executableDir, "Audio", "Particles.mp3");
                
                System.Diagnostics.Debug.WriteLine($"Executable directory: {executableDir}");
                System.Diagnostics.Debug.WriteLine($"Looking for audio file at: {relativePath}");
                
                if (System.IO.File.Exists(relativePath))
                {
                    audioUri = new Uri(relativePath, UriKind.Absolute);
                    System.Diagnostics.Debug.WriteLine($"✓ Found audio file, using path: {relativePath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Audio file not found at: {relativePath}");
                    
                    // Method 2: Try pack URI for embedded resource
                    audioUri = new Uri("pack://application:,,,/Audio/Particles.mp3");
                    System.Diagnostics.Debug.WriteLine("Trying pack URI for embedded resource");
                }

                // Load the audio file
                music.Open(audioUri);

                // Set up event handlers
                music.MediaOpened += Music_MediaOpened;
                music.MediaFailed += Music_MediaFailed;
                music.MediaEnded += Music_MediaEnded;

                // Set initial volume
                music.Volume = 0.5;  // 0.0 to 1.0
                
                isInitialized = true;
                System.Diagnostics.Debug.WriteLine("Audio manager initialized, waiting for MediaOpened event...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize audio: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                isInitialized = false;
            }
        }

        private void Music_MediaOpened(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎵 Media opened successfully! Starting playback...");
            music.Play();
        }

        private void Music_MediaFailed(object? sender, ExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Media failed to load: {e.ErrorException?.Message}");
            isInitialized = false;
        }

        private void Music_MediaEnded(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔄 Media ended, restarting loop...");
            music.Position = TimeSpan.Zero;
            music.Play();
        }

        private void DebugAudioPaths()
        {
            var executableDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            var audioDir = System.IO.Path.Combine(executableDir, "Audio");
            
            System.Diagnostics.Debug.WriteLine("=== Audio Debug Info ===");
            System.Diagnostics.Debug.WriteLine($"Executable location: {executableDir}");
            System.Diagnostics.Debug.WriteLine($"Audio directory: {audioDir}");
            System.Diagnostics.Debug.WriteLine($"Audio directory exists: {System.IO.Directory.Exists(audioDir)}");
            
            if (System.IO.Directory.Exists(audioDir))
            {
                var files = System.IO.Directory.GetFiles(audioDir);
                System.Diagnostics.Debug.WriteLine($"Files in Audio directory: {string.Join(", ", files)}");
            }
            
            var particlesPath = System.IO.Path.Combine(audioDir, "Particles.mp3");
            System.Diagnostics.Debug.WriteLine($"Particles.mp3 exists: {System.IO.File.Exists(particlesPath)}");
            System.Diagnostics.Debug.WriteLine("========================");
        }

        public void SetVolume(double volume)
        {
            if (isInitialized && music != null)
            {
                // Clamp volume between 0.0 and 1.0
                volume = Math.Max(0.0, Math.Min(1.0, volume));
                music.Volume = volume;
                System.Diagnostics.Debug.WriteLine($"🔊 Volume set to: {volume}");
            }
        }

        public void Play()
        {
            if (isInitialized && music != null)
            {
                music.Play();
                System.Diagnostics.Debug.WriteLine("▶️ Music play requested");
            }
        }

        public void Pause()
        {
            if (isInitialized && music != null)
            {
                music.Pause();
                System.Diagnostics.Debug.WriteLine("⏸️ Music paused");
            }
        }

        public void Stop()
        {
            if (isInitialized && music != null)
            {
                music.Stop();
                System.Diagnostics.Debug.WriteLine("⏹️ Music stopped");
            }
        }

        public bool IsPlaying 
        { 
            get 
            { 
                if (!isInitialized || music == null) return false;
                
                try
                {
                    return music.Position < music.NaturalDuration && music.CanPause;
                }
                catch
                {
                    return false;
                }
            }
        }
        
        public void Dispose()
        {
            if (music != null)
            {
                music.MediaOpened -= Music_MediaOpened;
                music.MediaFailed -= Music_MediaFailed;
                music.MediaEnded -= Music_MediaEnded;
                music.Stop();
                music.Close();
                music = null!;
            }
            isInitialized = false;
        }
    }
}
