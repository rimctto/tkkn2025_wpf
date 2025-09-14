using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Setting model for numeric integer values with min/max constraints
    /// </summary>
    public class IntegerSetting : SettingModelBase<int>
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
        /// Initialize a new integer setting
        /// </summary>
        /// <param name="name">Internal name/key</param>
        /// <param name="displayName">Display name for UI</param>
        /// <param name="category">Category grouping</param>
        /// <param name="defaultValue">Default integer value</param>
        /// <param name="min">Minimum allowed value</param>
        /// <param name="max">Maximum allowed value</param>
        /// <param name="description">Description of what this setting does</param>
        public IntegerSetting(string name, string displayName, string category, int defaultValue, int min, int max, string description = "")
            : base(name, displayName, category, defaultValue, description)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Strongly-typed access to the integer value (alias for Value property)
        /// </summary>
        public int IntValue
        {
            get => Value;
            set => Value = value;
        }
    }
}