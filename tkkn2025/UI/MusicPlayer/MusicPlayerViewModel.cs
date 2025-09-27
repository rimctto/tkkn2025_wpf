using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Diagnostics;
using System.Text;

namespace tkkn2025.UI.MusicPlayer
{
    /// <summary>
    /// MVVM ViewModel for the Music Player control
    /// Handles music playback, track management, and settings persistence
    /// </summary>
    public class MusicPlayerViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private DispatcherTimer _trackScrollTimer = new DispatcherTimer();
        private DispatcherTimer _positionTimer = new DispatcherTimer();
        
        private bool _isPlaying = false;
        private bool _repeatTrack = false;
        private string _currentTrackName = "No track";
        private int _currentTrackIndex = 0;
        private int _trackNameScrollPosition = 0;
        private string _displayedTrackName = "";
        private string _defaultTrack = "";
        
        private ObservableCollection<AudioTrack> _tracks = new ObservableCollection<AudioTrack>();

        #endregion

        #region Public Properties

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        } 

        public bool RepeatTrack
        {
            get => _repeatTrack;
            set => SetProperty(ref _repeatTrack, value);
        }

        public string CurrentTrackName
        {
            get => _currentTrackName;
            set => SetProperty(ref _currentTrackName, value);
        }

        public string DisplayedTrackName
        {
            get => _displayedTrackName;
            set => SetProperty(ref _displayedTrackName, value);
        }

        public string DefaultTrack
        {
            get => _defaultTrack;
            set => SetProperty(ref _defaultTrack, value);
        }

        public ObservableCollection<AudioTrack> Tracks
        {
            get => _tracks;
            set => SetProperty(ref _tracks, value);
        }

        public AudioTrack? CurrentTrack => _tracks.ElementAtOrDefault(_currentTrackIndex);

        #endregion

        #region Commands

        public ICommand PlayPauseCommand { get; private set; }
        public ICommand PreviousTrackCommand { get; private set; }
        public ICommand NextTrackCommand { get; private set; }
        public ICommand SetDefaultTrackCommand { get; private set; }

        #endregion

        #region Constructor

        public MusicPlayerViewModel()
        {
            InitializeCommands();
            InitializeTimers();
            ScanAudioFolder();
            InitializeMediaPlayer();
            LoadFromAppConfig();

        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            PlayPauseCommand = new RelayCommand(() => TogglePlayPause());
            PreviousTrackCommand = new RelayCommand(() => PlayPreviousTrack(), () => _tracks.Count > 1);
            NextTrackCommand = new RelayCommand(() => PlayNextTrack(), () => _tracks.Count > 1);
            SetDefaultTrackCommand = new RelayCommand(() => SetCurrentAsDefault(), () => CurrentTrack != null);
        }

        private void InitializeTimers()
        {
            // Timer for scrolling track name
            _trackScrollTimer.Interval = TimeSpan.FromMilliseconds(150);
            _trackScrollTimer.Tick += TrackScrollTimer_Tick;

            // Timer for updating position (if needed later)
            _positionTimer.Interval = TimeSpan.FromSeconds(1);
            _positionTimer.Tick += PositionTimer_Tick;
        }

        private void InitializeMediaPlayer()
        {
            _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            _mediaPlayer.Volume = 0.5;

            // Load default track (should be "Particles" if not set)
            //LoadDefaultTrack();
        }

        #endregion

        #region Audio Folder Scanning

        private void ScanAudioFolder()
        {
            try
            {
                var executableDir = AppDomain.CurrentDomain.BaseDirectory ?? "";
                var audioDir = Path.Combine(executableDir, "Audio");

                if (!Directory.Exists(audioDir))
                {
                    System.Diagnostics.Debug.WriteLine($"Audio directory not found: {audioDir}");
                    return;
                }

                var supportedExtensions = new[] { ".mp3", ".wav", ".wma", ".m4a" };
                var audioFiles = Directory.GetFiles(audioDir)
                    .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .ToArray();

                _tracks.Clear();
                foreach (var file in audioFiles)
                {
                    var track = new AudioTrack
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FilePath = file,
                        Duration = TimeSpan.Zero // Could be populated later if needed
                    };
                    _tracks.Add(track);
                }

                System.Diagnostics.Debug.WriteLine($"Found {_tracks.Count} audio files in {audioDir}");

                // Update current track name
                if (_tracks.Any())
                {
                    CurrentTrackName = CurrentTrack?.Name ?? "No track";
                    UpdateDisplayedTrackName();
                }
                else
                {
                    CurrentTrackName = "No tracks found";
                    DisplayedTrackName = CurrentTrackName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning audio folder: {ex.Message}");
                CurrentTrackName = "Error loading tracks";
                DisplayedTrackName = CurrentTrackName;
            }
        }

        #endregion

        #region Playback Control

        private void TogglePlayPause()
        {
            if (!_tracks.Any()) return;

            if (IsPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void Play()
        {
            if (!_tracks.Any()) return;

            try
            {
                var currentTrack = CurrentTrack;
                if (currentTrack != null && File.Exists(currentTrack.FilePath))
                {
                    _mediaPlayer.Open(new Uri(currentTrack.FilePath, UriKind.Absolute));
                    _mediaPlayer.Play();
                    IsPlaying = true;
                    _positionTimer.Start();
                    System.Diagnostics.Debug.WriteLine($"🎵 Playing: {currentTrack.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing track: {ex.Message}");
                IsPlaying = false;
            }
        }

        private void Pause()
        {
            try
            {
                _mediaPlayer.Pause();
                IsPlaying = false;
                _positionTimer.Stop();
                System.Diagnostics.Debug.WriteLine("⏸️ Music paused");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error pausing track: {ex.Message}");
            }
        }

        private void Stop()
        {
            try
            {
                _mediaPlayer.Stop();
                IsPlaying = false;
                _positionTimer.Stop();
                System.Diagnostics.Debug.WriteLine("⏹️ Music stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping track: {ex.Message}");
            }
        }

        private void PlayNextTrack()
        {
            if (!_tracks.Any()) return;

            _currentTrackIndex = (_currentTrackIndex + 1) % _tracks.Count;
            UpdateCurrentTrack();
            
            if (IsPlaying)
            {
                Play();
            }
        }

        private void PlayPreviousTrack()
        {
            if (!_tracks.Any()) return;

            _currentTrackIndex = (_currentTrackIndex - 1 + _tracks.Count) % _tracks.Count;
            UpdateCurrentTrack();
            
            if (IsPlaying)
            {
                Play();
            }
        }

        private void UpdateCurrentTrack()
        {
            CurrentTrackName = CurrentTrack?.Name ?? "No track";
            UpdateDisplayedTrackName();
            OnPropertyChanged(nameof(CurrentTrack));
            System.Diagnostics.Debug.WriteLine($"Current track: {CurrentTrackName}");
        }

        #endregion

        #region Track Name Scrolling

        private void UpdateDisplayedTrackName()
        {
            if (string.IsNullOrEmpty(CurrentTrackName))
            {
                DisplayedTrackName = "";
                _trackScrollTimer.Stop();
                return;
            }

            // Start scrolling if track name is longer than display area (approximate)
            if (CurrentTrackName.Length > 20)
            {
                _trackNameScrollPosition = 0;
                _trackScrollTimer.Start();
                UpdateScrollingText();
            }
            else
            {
                DisplayedTrackName = CurrentTrackName;
                _trackScrollTimer.Stop();
            }
        }

        private void TrackScrollTimer_Tick(object? sender, EventArgs e)
        {
            UpdateScrollingText();
        }

        private void UpdateScrollingText()
        {
            if (string.IsNullOrEmpty(CurrentTrackName)) return;

            const int displayLength = 100;
            const string separator = "                ";
            
            var scrollableText = CurrentTrackName + separator;
            
            if (_trackNameScrollPosition >= scrollableText.Length)
            {
                _trackNameScrollPosition = 0;
            }

            var displayText = "";
            for (int i = 0; i < displayLength; i++)
            {
                var charIndex = (_trackNameScrollPosition + i) % scrollableText.Length;
                displayText += scrollableText[charIndex];
            }

            DisplayedTrackName = displayText;
            _trackNameScrollPosition++;
        }

        #endregion

        #region Media Player Events

        private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎵 Media opened successfully");
        }

        private void MediaPlayer_MediaFailed(object? sender, ExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Media failed: {e.ErrorException?.Message}");
            IsPlaying = false;
        }

        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔄 Track ended");
            
            if (RepeatTrack)
            {
                // Restart the same track
                _mediaPlayer.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
            }
            else
            {
                // Move to next track
                PlayNextTrack();
            }
        }

        private void PositionTimer_Tick(object? sender, EventArgs e)
        {
            // Could be used for position updates, progress bars, etc.
        }

        #endregion

        #region Default Track Management

        private void LoadDefaultTrack()
        {
            // First, try to find "Particles" track as the preferred startup track
            string preferredStartupTrack = "Muffinz - Particles";
            
            if (_tracks.Any())
            {
                var particlesTrackIndex = _tracks.ToList().FindIndex(t => 
                    t.Name.Equals(preferredStartupTrack, StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains("Particles", StringComparison.OrdinalIgnoreCase));
                
                if (particlesTrackIndex >= 0)
                {
                    _currentTrackIndex = particlesTrackIndex;
                    System.Diagnostics.Debug.WriteLine($"Set startup track to: {_tracks[particlesTrackIndex].Name}");
                    
                    // Set as default if no default is currently set
                    if (string.IsNullOrEmpty(DefaultTrack))
                    {
                        DefaultTrack = _tracks[particlesTrackIndex].Name;
                    }
                }
                else if (!string.IsNullOrEmpty(DefaultTrack))
                {
                    // Fall back to the saved default track
                    var defaultTrackIndex = _tracks.ToList().FindIndex(t => t.Name == DefaultTrack);
                    if (defaultTrackIndex >= 0)
                    {
                        _currentTrackIndex = defaultTrackIndex;
                        System.Diagnostics.Debug.WriteLine($"Loaded saved default track: {DefaultTrack}");
                    }
                }
            }
            
            UpdateCurrentTrack();
        }

        public void SetCurrentAsDefault()
        {
            if (CurrentTrack != null)
            {
                DefaultTrack = CurrentTrack.Name;
                System.Diagnostics.Debug.WriteLine($"Set default track: {DefaultTrack}");
            }
        }

        #endregion

        #region AppConfig Integration

        private void LoadFromAppConfig()
        {
            try
            {
                var appConfig = ConfigManager.LoadAppConfig();
                
                RepeatTrack = appConfig.RepeatTrack;
                DefaultTrack = appConfig.DefaultTrack;
                IsPlaying = appConfig.IsPlaying;
                
                if (IsPlaying)
                {
                    Play();
                }
                System.Diagnostics.Debug.WriteLine($"Music settings loaded - Repeat: {RepeatTrack}, Default: {DefaultTrack}, Was Playing: {IsPlaying}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading music settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the AppConfig with current music player state
        /// This should be called by the external application when it wants to save state
        /// </summary>
        public void UpdateAppConfig(AppConfig appConfig)
        {
            appConfig.RepeatTrack = RepeatTrack;
            appConfig.DefaultTrack = DefaultTrack;
            appConfig.IsPlaying = IsPlaying;
        }

        #endregion

        #region Volume Control

        public void SetVolume(double volume)
        {
            try
            {
                volume = Math.Max(0.0, Math.Min(1.0, volume));
                _mediaPlayer.Volume = volume;
                System.Diagnostics.Debug.WriteLine($"🔊 Volume set to: {volume:P0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting volume: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }

    #region Helper Classes

    /// <summary>
    /// Represents an audio track
    /// </summary>
    public class AudioTrack
    {
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Simple RelayCommand implementation for MVVM commands
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    #endregion
}
