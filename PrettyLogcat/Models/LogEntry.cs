using System;
using System.ComponentModel;
using System.Linq;
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
        private bool _isPinned = false;
        private int _originalIndex = -1;
        private bool _isMerged = false;
        private bool _isExpanded = false;

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

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                _isPinned = value;
                OnPropertyChanged(nameof(IsPinned));
            }
        }

        public int OriginalIndex
        {
            get => _originalIndex;
            set
            {
                _originalIndex = value;
                OnPropertyChanged(nameof(OriginalIndex));
            }
        }

        public bool IsMerged
        {
            get => _isMerged;
            set
            {
                _isMerged = value;
                OnPropertyChanged(nameof(IsMerged));
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(DisplayMessage));
                OnPropertyChanged(nameof(IsMultiLine));
                OnPropertyChanged(nameof(CanExpand));
            }
        }

        // 计算属性：是否是多行日志
        public bool IsMultiLine => Message.Contains(Environment.NewLine);

        // 计算属性：是否可以展开（多行且当前未展开）
        public bool CanExpand => IsMultiLine && !IsExpanded;

        // 计算属性：是否可以收起（多行且当前已展开）
        public bool CanCollapse => IsMultiLine && IsExpanded;

        // 静态属性用于配置行数限制
        public static int PreviewLineLimit { get; set; } = 3;

        // 计算属性：显示的消息内容
        public string DisplayMessage
        {
            get
            {
                if (!IsMultiLine)
                    return Message;

                if (IsExpanded)
                    return Message;

                // 显示前N行（可配置）
                var lines = Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                if (lines.Length <= PreviewLineLimit)
                    return Message;

                return string.Join(Environment.NewLine, lines.Take(PreviewLineLimit)) + Environment.NewLine + "... ▼";
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