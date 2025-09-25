using System;
using System.ComponentModel;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Base class for enum settings (non-generic for XAML compatibility)
    /// </summary>
    public abstract class EnumSetting : SettingModelBase<object>
    {
        public abstract Array Options { get; }

        protected EnumSetting(string name, string displayName, string category, object defaultValue, string description)
            : base(name, displayName, category, defaultValue, description)
        {
        }
    }

    /// <summary>
    /// Generic enum setting that provides type-safe access to enum values
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    public class EnumSetting<T> : EnumSetting where T : struct, Enum
    {
        /// <summary>
        /// Array of all possible enum values for binding to ComboBox
        /// </summary>
        public override Array Options => Enum.GetValues(typeof(T));

        /// <summary>
        /// Strongly-typed access to the enum value
        /// </summary>
        public new T Value
        {
            get => (T)base.Value;
            set => base.Value = value;
        }

        /// <summary>
        /// Strongly-typed default value
        /// </summary>
        public new T DefaultValue => (T)base.DefaultValue;

        public EnumSetting(string name, string displayName, string category, T defaultValue, string description = "")
            : base(name, displayName, category, defaultValue, description)
        {
        }

        /// <summary>
        /// Implicit conversion to the enum type for easier usage
        /// </summary>
        /// <param name="setting">The enum setting</param>
        public static implicit operator T(EnumSetting<T> setting) => setting.Value;
    }
}