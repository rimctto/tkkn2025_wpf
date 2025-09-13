namespace tkkn2025.Settings
{
    /// <summary>
    /// Setting model for numeric integer values with min/max constraints
    /// </summary>
    public class NumericSetting : SettingModelBase
    {
        /// <summary>
        /// Minimum allowed value
        /// </summary>
        public int Min { get; }

        /// <summary>
        /// Maximum allowed value
        /// </summary>
        public int Max { get; }

        /// <summary>
        /// Initialize a new numeric setting
        /// </summary>
        /// <param name="name">Internal name/key</param>
        /// <param name="displayName">Display name for UI</param>
        /// <param name="category">Category grouping</param>
        /// <param name="defaultValue">Default integer value</param>
        /// <param name="min">Minimum allowed value</param>
        /// <param name="max">Maximum allowed value</param>
        public NumericSetting(string name, string displayName, string category, int defaultValue, int min, int max)
            : base(name, displayName, category, defaultValue)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Strongly-typed access to the integer value
        /// </summary>
        public int IntValue
        {
            get => (int)Value;
            set => Value = value;
        }
    }
}