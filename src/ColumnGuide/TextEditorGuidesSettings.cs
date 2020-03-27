// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.Text;

using static System.Globalization.CultureInfo;
using static ColumnGuide.Guideline;

namespace ColumnGuide
{
    [Export(typeof(ITextEditorGuidesSettings))]
    [Export(typeof(ITextEditorGuidesSettingsChanger))]
    internal sealed class TextEditorGuidesSettings : ITextEditorGuidesSettings, INotifyPropertyChanged, ITextEditorGuidesSettingsChanger
    {
        private const int c_maxGuides = 12;

        [Import]
        private Lazy<HostServices> HostServices { get; set; }

        private IVsSettingsStore ReadOnlyUserSettings
        {
            get
            {
                var manager = HostServices.Value.SettingsManagerService;
                Marshal.ThrowExceptionForHR(manager.GetReadOnlySettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out var store));
                return store;
            }
        }

        private IVsWritableSettingsStore ReadWriteUserSettings
        {
            get
            {
                var manager = HostServices.Value.SettingsManagerService;
                Marshal.ThrowExceptionForHR(manager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out var store));
                return store;
            }
        }


        private string GetUserSettingsString(string key, string value)
        {
            var store = ReadOnlyUserSettings;
            Marshal.ThrowExceptionForHR(store.GetStringOrDefault(key, value, string.Empty, out var result));
            return result;
        }

        private void WriteUserSettingsString(string key, string propertyName, string value)
        {
            var store = ReadWriteUserSettings;
            Marshal.ThrowExceptionForHR(store.SetString(key, propertyName, value));
        }

        private void WriteSettings(Color color, IEnumerable<int> columns)
        {
            var value = ComposeSettingsString(color, columns);
            GuidelinesConfiguration = value;
        }

        private static string ComposeSettingsString(Color color, IEnumerable<int> columns)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(InvariantCulture, "RGB({0},{1},{2})", color.R, color.G, color.B);
            var columnsEnumerator = columns.GetEnumerator();
            if( columnsEnumerator.MoveNext() )
            {
                sb.AppendFormat(InvariantCulture, " {0}", columnsEnumerator.Current);
                while( columnsEnumerator.MoveNext() )
                {
                    sb.AppendFormat(InvariantCulture, ", {0}", columnsEnumerator.Current);
                }
            }

            return sb.ToString();
        }

        #region ITextEditorGuidesSettingsChanger Members

        public bool AddGuideline(int column)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException(nameof(column), Resources.AddGuidelineParameterOutOfRange);
            }

            if (GetCountOfGuidelines() >= c_maxGuides)
            {
                return false; // Cannot add more than _maxGuides guidelines
            }

            // Check for duplicates
            var columns = new List<int>(GuideLinePositionsInChars);
            if (columns.Contains(column))
            {
                return false;
            }

            columns.Add(column);

            WriteSettings(GuidelinesColor, columns);
            return true;
        }

        public bool RemoveGuideline(int column)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException(nameof(column), Resources.RemoveGuidelineParameterOutOfRange);
            }

            var columns = new List<int>(GuideLinePositionsInChars);
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

            WriteSettings(GuidelinesColor, columns);
            return true;
        }

        public bool CanAddGuideline(int column)
            => IsValidColumn(column)
            && GetCountOfGuidelines() < c_maxGuides
            && !IsGuidelinePresent(column);

        public bool CanRemoveGuideline(int column)
            => IsValidColumn(column)
            && (IsGuidelinePresent(column) || HasExactlyOneGuideline()); // Allow user to remove the last guideline regardless of the column

        public void RemoveAllGuidelines()
            => WriteSettings(GuidelinesColor, Array.Empty<int>());

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
            var i = 0;
            foreach (var value in GuideLinePositionsInChars)
            {
                i++;
            }
            return i;
        }

        private bool IsGuidelinePresent(int column)
        {
            foreach (var value in GuideLinePositionsInChars)
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
                    _guidelinesConfiguration = GetUserSettingsString(c_textEditor, "Guides").Trim();
                }
                return _guidelinesConfiguration;
            }

            set
            {
                if (value != _guidelinesConfiguration)
                {
                    _guidelinesConfiguration = value;
                    WriteUserSettingsString(c_textEditor, "Guides", value);
                    FirePropertyChanged(nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars));
                }
            }
        }

        private void FirePropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Parse a color out of a string that begins like "RGB(255,0,0)"
        public Color GuidelinesColor
        {
            get
            {
                var config = GuidelinesConfiguration;
                if (!string.IsNullOrEmpty(config) && config.StartsWith("RGB(", StringComparison.Ordinal))
                {
                    var lastParen = config.IndexOf(')');
                    if (lastParen > 4)
                    {
                        var rgbs = config.Substring(4, lastParen - 4).Split(',');

                        if (rgbs.Length >= 3)
                        {
                            if (byte.TryParse(rgbs[0], out var r) &&
                                byte.TryParse(rgbs[1], out var g) &&
                                byte.TryParse(rgbs[2], out var b))
                            {
                                return Color.FromRgb(r, g, b);
                            }
                        }
                    }
                }
                return Colors.DarkRed;
            }

            set => WriteSettings(value, GuideLinePositionsInChars);
        }

        // Parse a list of integer values out of a string that looks like "RGB(255,0,0) 1,5,10,80"
        public IEnumerable<int> GuideLinePositionsInChars
        {
            get
            {
                var config = GuidelinesConfiguration;
                if (string.IsNullOrEmpty(config))
                {
                    yield break;
                }
                if (!config.StartsWith("RGB(", StringComparison.Ordinal))
                {
                    yield break;
                }

                var lastParen = config.IndexOf(')');
                if (lastParen <= 4)
                {
                    yield break;
                }

                var columns = config.Substring(lastParen + 1).Split(',');

                var columnCount = 0;
                foreach (var columnText in columns)
                {
                    var column = -1;
                    if (int.TryParse(columnText, out column) && column >= 0 /*Note: VS 2008 didn't allow zero, but we do, per user request*/ )
                    {
                        columnCount++;
                        yield return column;
                        if (columnCount >= c_maxGuides)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public bool DontShowVsVersionWarning
        {
            get
            {
                var store = ReadOnlyUserSettings;
                Marshal.ThrowExceptionForHR(store.GetBoolOrDefault(c_textEditor, c_dontShowVsVersionWarningPropertyName, 0, out int value));
                return value != 0;
            }

            set
            {
                var store = ReadWriteUserSettings;
                Marshal.ThrowExceptionForHR(store.SetBool(c_textEditor, c_dontShowVsVersionWarningPropertyName, value ? 1 : 0));
            }
        }

        private const string c_textEditor = "Text Editor";
        private const string c_dontShowVsVersionWarningPropertyName = "DontShowEditorGuidelinesVsVersionWarning";

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
