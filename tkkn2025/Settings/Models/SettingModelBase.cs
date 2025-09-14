using System.ComponentModel;

namespace tkkn2025.Settings.Models
{
   
    public interface ISettingModel
    {
        string Name { get; }
        string DisplayName { get; }
        string Category { get; }
        object Value { get; set; }
        object DefaultValue { get; }
        string Description { get; }
        event PropertyChangedEventHandler? PropertyChanged;
    }

    public abstract class SettingModelBase<T> : ISettingModel, INotifyPropertyChanged
    {
        public string Name { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Description { get; }

        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        public T DefaultValue { get; }

        object ISettingModel.Value
        {
            get => Value!;
            set => Value = (T)value;
        }

        object ISettingModel.DefaultValue => DefaultValue!;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected SettingModelBase(string name, string displayName, string category, T defaultValue, string description)
        {
            Name = name;
            DisplayName = displayName;
            Category = category;
            _value = defaultValue;
            DefaultValue = defaultValue;
            Description = description;
        }

        // 🔹 Implicit conversion TO T
        public static implicit operator T(SettingModelBase<T> setting) => setting.Value;

        // 🔹 Implicit assignment FROM T (updates existing instance)
        public static implicit operator SettingModelBase<T>(T value)
        {
            throw new InvalidOperationException(
                "Cannot assign a raw value to a SettingModelBase<T> without referencing an instance. " +
                "Use a wrapper property or method to update the Value instead.");
        }

       
        public void Set(T value) => Value = value;
    }
}
