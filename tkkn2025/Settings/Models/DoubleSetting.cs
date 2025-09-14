using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Setting model for numeric double values with min/max constraints
    /// </summary>
    public class DoubleSetting : SettingModelBase<double>
    {
        
        public double Min { get; }

     
        public double Max { get; }

        
        public DoubleSetting(string name, string displayName, string category, double defaultValue, double min, double max, string description = "")
            : base(name, displayName, category, defaultValue, description)
        {
            Min = min;
            Max = max;
        }

      
    }
}