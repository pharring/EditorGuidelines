// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace ColumnGuide
{
    [Export(typeof(EditorFormatDefinition)), UserVisible(true), Name(c_name)]
    public class GuidelineColorDefinition : EditorFormatDefinition
    {
        internal const string c_name = "Guideline";

        public GuidelineColorDefinition()
        {
            DisplayName = c_name;
            ForegroundCustomizable = false;
            BackgroundColor = Colors.DarkRed;
        }
    }
}
