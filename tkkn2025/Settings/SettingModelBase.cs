using System.ComponentModel;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Abstract base class for all setting models with property change notification
    /// </summary>
    public abstract class SettingModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// The internal name/key of the setting
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The display name shown in the UI
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The category this setting belongs to
        /// </summary>
        public string Category { get; }

        private object _value;
        /// <summary>
        /// The current value of the setting
        /// </summary>
        public object Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        /// <summary>
        /// The default value of the setting
        /// </summary>
        public object DefaultValue { get; }

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initialize a new setting model
        /// </summary>
        /// <param name="name">Internal name/key</param>
        /// <param name="displayName">Display name for UI</param>
        /// <param name="category">Category grouping</param>
        /// <param name="defaultValue">Default value</param>
        protected SettingModelBase(string name, string displayName, string category, object defaultValue)
        {
            Name = name;
            DisplayName = displayName;
            Category = category;
            Value = DefaultValue = defaultValue;
        }
    }
}