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
