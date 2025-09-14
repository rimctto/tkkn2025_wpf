using System;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Base class for enum settings (non-generic for XAML compatibility)
    /// </summary>
    public abstract class EnumSetting : ISettingModel
    {
        /// <summary>
        /// All available options for this enum type
        /// </summary>
        public abstract Array Options { get; }

        /// <summary>
        /// The internal name/key of the setting
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The display name shown in the UI
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// The category this setting belongs to
        /// </summary>
        public abstract string Category { get; }

        /// <summary>
        /// Non-generic access to value
        /// </summary>
        public abstract object Value { get; set; }

        /// <summary>
        /// Non-generic access to default value
        /// </summary>
        public abstract object DefaultValue { get; }

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public abstract event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// Setting model for enumeration values
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    public class EnumSetting<T> : SettingModelBase<T> where T : struct, Enum
    {
        /// <summary>
        /// Initialize a new enum setting
        /// </summary>
        /// <param name="name">Internal name/key</param>
        /// <param name="displayName">Display name for UI</param>
        /// <param name="category">Category grouping</param>
        /// <param name="defaultValue">Default enum value</param>
        public EnumSetting(string name, string displayName, string category, T defaultValue)
            : base(name, displayName, category, defaultValue) { }

        /// <summary>
        /// Strongly-typed access to the enum value (alias for Value property)
        /// </summary>
        public T EnumValue
        {
            get => Value;
            set => Value = value;
        }

        /// <summary>
        /// All available options for this enum type
        /// </summary>
        public Array Options => Enum.GetValues(typeof(T));
    }
}