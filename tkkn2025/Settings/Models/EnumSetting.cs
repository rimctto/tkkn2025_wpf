using System;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Base class for enum settings (non-generic for XAML compatibility)
    /// </summary>
    public abstract class EnumSetting : ISettingModel
    {
  
        public abstract Array Options { get; }

        public abstract string Name { get; }

        public abstract string DisplayName { get; }

        public abstract string Category { get; }

        public abstract string Description { get; }

     
        public abstract object Value { get; set; }

        public abstract object DefaultValue { get; }

        public abstract event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    public class EnumSetting<T> : SettingModelBase<T> where T : struct, Enum
    {
       
        public EnumSetting(string name, string displayName, string category, T defaultValue, string description = "")
            : base(name, displayName, category, defaultValue, description) { }

      
        public Array Options => Enum.GetValues(typeof(T));
    }
}