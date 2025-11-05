using System;
using System.ComponentModel;
using System.Windows.Media;

namespace PrettyLogcat.Models
{
    public class LogEntry : INotifyPropertyChanged
    {
        private DateTime _timeStamp;
        private LogLevel _level;
        private int _pid;
        private int _tid;
        private string _tag = string.Empty;
        private string _message = string.Empty;
        private string _rawLine = string.Empty;

        public DateTime TimeStamp
        {
            get => _timeStamp;
            set
            {
                _timeStamp = value;
                OnPropertyChanged(nameof(TimeStamp));
            }
        }

        public LogLevel Level
        {
            get => _level;
            set
            {
                _level = value;
                OnPropertyChanged(nameof(Level));
                OnPropertyChanged(nameof(LevelBrush));
            }
        }

        public int Pid
        {
            get => _pid;
            set
            {
                _pid = value;
                OnPropertyChanged(nameof(Pid));
            }
        }

        public int Tid
        {
            get => _tid;
            set
            {
                _tid = value;
                OnPropertyChanged(nameof(Tid));
            }
        }

        public string Tag
        {
            get => _tag;
            set
            {
                _tag = value ?? string.Empty;
                OnPropertyChanged(nameof(Tag));
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value ?? string.Empty;
                OnPropertyChanged(nameof(Message));
            }
        }

        public string RawLine
        {
            get => _rawLine;
            set
            {
                _rawLine = value ?? string.Empty;
                OnPropertyChanged(nameof(RawLine));
            }
        }

        public Brush LevelBrush
        {
            get
            {
                return Level switch
                {
                    LogLevel.Verbose => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),
                    LogLevel.Debug => new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),
                    LogLevel.Info => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                    LogLevel.Warn => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),
                    LogLevel.Error => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),
                    LogLevel.Fatal => new SolidColorBrush(Color.FromRgb(0x9C, 0x27, 0xB0)),
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{TimeStamp:MM-dd HH:mm:ss.fff} {Pid,5} {Tid,5} {Level} {Tag}: {Message}";
        }
    }

    public enum LogLevel
    {
        Verbose = 2,
        Debug = 3,
        Info = 4,
        Warn = 5,
        Error = 6,
        Fatal = 7
    }
}