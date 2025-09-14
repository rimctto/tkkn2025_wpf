namespace tkkn2025.Settings
{
    /// <summary>
    /// Setting model for numeric double values with min/max constraints
    /// </summary>
    public class DoubleSetting : SettingModelBase<double>
    {
        /// <summary>
        /// Minimum allowed value
        /// </summary>
        public double Min { get; }

        /// <summary>
        /// Maximum allowed value
        /// </summary>
        public double Max { get; }

        /// <summary>
        /// Initialize a new double setting
        /// </summary>
        /// <param name="name">Internal name/key</param>
        /// <param name="displayName">Display name for UI</param>
        /// <param name="category">Category grouping</param>
        /// <param name="defaultValue">Default double value</param>
        /// <param name="min">Minimum allowed value</param>
        /// <param name="max">Maximum allowed value</param>
        public DoubleSetting(string name, string displayName, string category, double defaultValue, double min, double max)
            : base(name, displayName, category, defaultValue)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Strongly-typed access to the double value (alias for Value property)
        /// </summary>
        public double DoubleValue
        {
            get => Value;
            set => Value = value;
        }
    }
}