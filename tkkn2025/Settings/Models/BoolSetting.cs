using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
   
    public class BoolSetting : SettingModelBase<bool>
    {
    
        public BoolSetting(string name, string displayName, string category, bool defaultValue, string description)
            : base(name, displayName, category, defaultValue, description) { }
       
    }
}