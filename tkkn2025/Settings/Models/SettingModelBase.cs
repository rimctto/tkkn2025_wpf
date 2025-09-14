using System.ComponentModel;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Non-generic base interface for all setting models (for collection compatibility)
    /// </summary>
    public interface ISettingModel
    {
        string Name { get; }
        string DisplayName { get; }
        string Category { get; }
        object Value { get; set; }
        object DefaultValue { get; }
        event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// Abstract base class for all setting models with property change notification
    /// </summary>
    /// <typeparam name="T">The type of value this setting contains</typeparam>
    public abstract class SettingModelBase<T> : ISettingModel, INotifyPropertyChanged
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

        private T _value;
        /// <summary>
        /// The current value of the setting
        /// </summary>
        public T Value
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
        public T DefaultValue { get; }

        /// <summary>
        /// Non-generic access to value (for interface compatibility)
        /// </summary>
        object ISettingModel.Value
        {
            get => Value!;
            set => Value = (T)value;
        }

        /// <summary>
        /// Non-generic access to default value (for interface compatibility)
        /// </summary>
        object ISettingModel.DefaultValue => DefaultValue!;

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
        protected SettingModelBase(string name, string displayName, string category, T defaultValue)
        {
            Name = name;
            DisplayName = displayName;
            Category = category;
            _value = defaultValue;
            DefaultValue = defaultValue;
        }
    }
}