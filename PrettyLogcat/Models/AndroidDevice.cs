using System.ComponentModel;

namespace PrettyLogcat.Models
{
    public class AndroidDevice : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private DeviceState _state;
        private string _model = string.Empty;
        private string _product = string.Empty;
        private string _device = string.Empty;

        public string Id
        {
            get => _id;
            set
            {
                _id = value ?? string.Empty;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value ?? string.Empty;
                OnPropertyChanged(nameof(Name));
            }
        }

        public DeviceState State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(IsOnline));
            }
        }

        public string Model
        {
            get => _model;
            set
            {
                _model = value ?? string.Empty;
                OnPropertyChanged(nameof(Model));
                UpdateName();
            }
        }

        public string Product
        {
            get => _product;
            set
            {
                _product = value ?? string.Empty;
                OnPropertyChanged(nameof(Product));
                UpdateName();
            }
        }

        public string Device
        {
            get => _device;
            set
            {
                _device = value ?? string.Empty;
                OnPropertyChanged(nameof(Device));
                UpdateName();
            }
        }

        public bool IsOnline => State == DeviceState.Device;

        private void UpdateName()
        {
            if (!string.IsNullOrEmpty(Model))
            {
                Name = $"{Model} ({Id})";
            }
            else if (!string.IsNullOrEmpty(Product))
            {
                Name = $"{Product} ({Id})";
            }
            else
            {
                Name = Id;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object? obj)
        {
            return obj is AndroidDevice device && Id == device.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public enum DeviceState
    {
        Unknown,
        Offline,
        Device,
        Unauthorized,
        Bootloader,
        Recovery
    }
}