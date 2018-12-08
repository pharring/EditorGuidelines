// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace ColumnGuide
{
    [Export(typeof(EditorFormatDefinition)), UserVisible(true), Name(GuidelineColorDefinition.Name)]
    public class GuidelineColorDefinition : EditorFormatDefinition
    {
        internal const string Name = "Guideline";

        public GuidelineColorDefinition()
        {
            this.DisplayName = "Guideline";
            this.ForegroundCustomizable = false;
            this.BackgroundColor = Colors.DarkRed;
        }
    }
}
