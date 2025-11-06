using System.Collections.Generic;

namespace PrettyLogcat.Services
{
    public interface ISettingsService
    {
        // Column visibility settings
        bool ShowTimeColumn { get; set; }
        bool ShowLevelColumn { get; set; }
        bool ShowPidColumn { get; set; }
        bool ShowTidColumn { get; set; }
        bool ShowTagColumn { get; set; }
        bool ShowMessageColumn { get; set; }

        // Filter settings
        bool ShowVerbose { get; set; }
        bool ShowDebug { get; set; }
        bool ShowInfo { get; set; }
        bool ShowWarn { get; set; }
        bool ShowError { get; set; }
        bool ShowFatal { get; set; }

        // Filter values
        string TagFilter { get; set; }
        string MessageFilter { get; set; }
        string PidFilter { get; set; }

        // Filter history (max 8 items each)
        List<string> TagFilterHistory { get; set; }
        List<string> MessageFilterHistory { get; set; }
        List<string> PidFilterHistory { get; set; }

        /// <summary>
        /// Load settings from storage
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// Save settings to storage
        /// </summary>
        void SaveSettings();

        /// <summary>
        /// Add filter to history and maintain max count
        /// </summary>
        void AddToFilterHistory(Models.FilterType filterType, string value);
    }
}