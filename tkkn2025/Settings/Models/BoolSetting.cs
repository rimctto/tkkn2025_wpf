namespace tkkn2025.Settings
{
    /// <summary>
    /// Setting model for boolean values (true/false)
    /// </summary>
    public class BoolSetting : SettingModelBase<bool>
    {
        /// <summary>
        /// Initialize a new boolean setting
        /// </summary>
        /// <param name="name">Internal name/key</param>
        /// <param name="displayName">Display name for UI</param>
        /// <param name="category">Category grouping</param>
        /// <param name="defaultValue">Default boolean value</param>
        public BoolSetting(string name, string displayName, string category, bool defaultValue)
            : base(name, displayName, category, defaultValue) { }

        /// <summary>
        /// Strongly-typed access to the boolean value (alias for Value property)
        /// </summary>
        public bool BoolValue
        {
            get => Value;
            set => Value = value;
        }
    }
}