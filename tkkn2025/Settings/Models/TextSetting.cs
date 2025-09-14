using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Setting model for text/string values
    /// </summary>
    public class TextSetting : SettingModelBase<string>
    {
        /// <summary>
        /// Initialize a new text setting
        /// </summary>
        /// <param name="name">Internal name/key</param>
        /// <param name="displayName">Display name for UI</param>
        /// <param name="category">Category grouping</param>
        /// <param name="defaultValue">Default string value</param>
        /// <param name="description">Description of what this setting does</param>
        public TextSetting(string name, string displayName, string category, string defaultValue, string description = "")
            : base(name, displayName, category, defaultValue, description) { }

        /// <summary>
        /// Strongly-typed access to the string value (alias for Value property)
        /// </summary>
        public string TextValue
        {
            get => Value;
            set => Value = value;
        }
    }
}