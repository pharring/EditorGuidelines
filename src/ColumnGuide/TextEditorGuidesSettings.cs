using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.Text;

namespace ColumnGuide
{
    [Export(typeof(ITextEditorGuidesSettings))]
    [Export(typeof(ITextEditorGuidesSettingsChanger))]
    sealed class TextEditorGuidesSettings : ITextEditorGuidesSettings, INotifyPropertyChanged, ITextEditorGuidesSettingsChanger
    {
        private const int _maxGuides = 12;

        [Import]
        Lazy<HostServices> HostServices { get; set; }

        IVsSettingsStore ReadOnlyUserSettings
        {
            get
            {
                IVsSettingsManager manager = HostServices.Value.SettingsManagerService;
                Marshal.ThrowExceptionForHR(manager.GetReadOnlySettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out var store));
                return store;
            }
        }

        IVsWritableSettingsStore ReadWriteUserSettings
        {
            get
            {
                IVsSettingsManager manager = HostServices.Value.SettingsManagerService;
                Marshal.ThrowExceptionForHR(manager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out var store));
                return store;
            }
        }


        private string GetUserSettingsString(string key, string value)
        {
            IVsSettingsStore store = ReadOnlyUserSettings;
            Marshal.ThrowExceptionForHR(store.GetStringOrDefault(key, value, String.Empty, out string result));
            return result;
        }

        private void WriteUserSettingsString(string key, string propertyName, string value)
        {
            IVsWritableSettingsStore store = ReadWriteUserSettings;
            Marshal.ThrowExceptionForHR(store.SetString(key, propertyName, value));
        }

        private void WriteSettings(Color color, IEnumerable<int> columns)
        {
            string value = ComposeSettingsString(color, columns);
            GuidelinesConfiguration = value;
        }

        private static string ComposeSettingsString(Color color, IEnumerable<int> columns)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("RGB({0},{1},{2})", color.R, color.G, color.B);
            IEnumerator<int> columnsEnumerator = columns.GetEnumerator();
            if( columnsEnumerator.MoveNext() )
            {
                sb.AppendFormat(" {0}", columnsEnumerator.Current);
                while( columnsEnumerator.MoveNext() )
                {
                    sb.AppendFormat(", {0}", columnsEnumerator.Current);
                }
            }

            return sb.ToString();
        }

        #region ITextEditorGuidesSettingsChanger Members

        public bool AddGuideline(int column)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException("column", "The paramenter must be between 1 and 10,000");
            }

            if (GetCountOfGuidelines() >= _maxGuides)
            {
                return false; // Cannot add more than _maxGuides guidelines
            }

            // Check for duplicates
            List<int> columns = new List<int>(GuideLinePositionsInChars);
            if (columns.Contains(column))
            {
                return false;
            }

            columns.Add(column);

            WriteSettings(this.GuidelinesColor, columns);
            return true;
        }

        public bool RemoveGuideline(int column)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException("column", "The paramenter must be between 1 and 10,000");
            }

            List<int> columns = new List<int>(GuideLinePositionsInChars);
            if (!columns.Remove(column))
            {
                // Not present
                // Allow user to remove the last column even if they're not on the right column
                if (columns.Count != 1)
                {
                    return false;
                }

                columns.Clear();
            }

            WriteSettings(this.GuidelinesColor, columns);
            return true;
        }

        public bool CanAddGuideline(int column)
        {
            if (!IsValidColumn(column))
            {
                return false;
            }

            if (GetCountOfGuidelines() >= _maxGuides)
            {
                return false;
            }

            return !IsGuidelinePresent(column);
        }

        public bool CanRemoveGuideline(int column)
        {
            if (!IsValidColumn(column))
            {
                return false;
            }

            return IsGuidelinePresent(column)
                || HasExactlyOneGuideline(); // Allow user to remove the last guideline regardless of the column
        }

        public void RemoveAllGuidelines()
        {
            WriteSettings(this.GuidelinesColor, new int[0]);
        }

        #endregion

        private bool HasExactlyOneGuideline()
        {
            using (var enumerator = GuideLinePositionsInChars.GetEnumerator())
            {
                return enumerator.MoveNext() && !enumerator.MoveNext();
            }
        }

        private int GetCountOfGuidelines()
        {
            int i = 0;
            foreach (int value in GuideLinePositionsInChars)
            {
                i++;
            }
            return i;
        }

        private static bool IsValidColumn(int column)
        {
            // -ve is not allowed
            // zero is allowed (per user request)
            // 10000 seems like a sensible upper limit
            return 0 <= column && column <= 10000;
        }

        private bool IsGuidelinePresent(int column)
        {
            foreach (int value in GuideLinePositionsInChars)
            {
                if (value == column)
                {
                    return true;
                }
            }

            return false;
        }

        private string _guidelinesConfiguration;
        private string GuidelinesConfiguration
        {
            get
            {
                if (_guidelinesConfiguration == null)
                {
                    _guidelinesConfiguration = GetUserSettingsString("Text Editor", "Guides").Trim();
                }
                return _guidelinesConfiguration;
            }

            set
            {
                if (value != _guidelinesConfiguration)
                {
                    _guidelinesConfiguration = value;
                    WriteUserSettingsString("Text Editor", "Guides", value);
                    FirePropertyChanged(nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars));
                }
            }
        }

        private void FirePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Parse a color out of a string that begins like "RGB(255,0,0)"
        public Color GuidelinesColor
        {
            get
            {
                string config = GuidelinesConfiguration;
                if (!String.IsNullOrEmpty(config) && config.StartsWith("RGB(", StringComparison.Ordinal))
                {
                    int lastParen = config.IndexOf(')');
                    if (lastParen > 4)
                    {
                        string[] rgbs = config.Substring(4, lastParen - 4).Split(',');

                        if (rgbs.Length >= 3)
                        {
                            if (byte.TryParse(rgbs[0], out byte r) &&
                                byte.TryParse(rgbs[1], out byte g) &&
                                byte.TryParse(rgbs[2], out byte b))
                            {
                                return Color.FromRgb(r, g, b);
                            }
                        }
                    }
                }
                return Colors.DarkRed;
            }

            set
            {
                WriteSettings(value, GuideLinePositionsInChars);
            }
        }

        // Parse a list of integer values out of a string that looks like "RGB(255,0,0) 1,5,10,80"
        public IEnumerable<int> GuideLinePositionsInChars
        {
            get
            {
                string config = GuidelinesConfiguration;
                if (String.IsNullOrEmpty(config))
                {
                    yield break;
                }
                if (!config.StartsWith("RGB(", StringComparison.Ordinal))
                {
                    yield break;
                }

                int lastParen = config.IndexOf(')');
                if (lastParen <= 4)
                {
                    yield break;
                }

                string[] columns = config.Substring(lastParen + 1).Split(',');

                int columnCount = 0;
                foreach (string columnText in columns)
                {
                    int column = -1;
                    if (int.TryParse(columnText, out column) && column >= 0 /*Note: VS 2008 didn't allow zero, but we do, per user request*/ )
                    {
                        columnCount++;
                        yield return column;
                        if (columnCount >= _maxGuides)
                        {
                            break;
                        }
                    }
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
