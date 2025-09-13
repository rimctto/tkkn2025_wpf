namespace tkkn2025.Settings
{
    /// <summary>
    /// Setting model for text/string values
    /// </summary>
    public class TextSetting : SettingModelBase
    {
        /// <summary>
        /// Initialize a new text setting
        /// </summary>
        /// <param name="name">Internal name/key</param>
        /// <param name="displayName">Display name for UI</param>
        /// <param name="category">Category grouping</param>
        /// <param name="defaultValue">Default string value</param>
        public TextSetting(string name, string displayName, string category, string defaultValue)
            : base(name, displayName, category, defaultValue) { }

        /// <summary>
        /// Strongly-typed access to the string value
        /// </summary>
        public string TextValue
        {
            get => (string)Value;
            set => Value = value;
        }
    }
}