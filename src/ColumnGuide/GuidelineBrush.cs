// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.VisualStudio.Text.Classification;
using System;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace ColumnGuide
{
    [Export]
    internal class GuidelineBrush
    {
        private Brush _brush;
        private readonly IEditorFormatMap _formatMap;

        [ImportingConstructor]
        private GuidelineBrush(IEditorFormatMapService editorFormatMapService)
        {
            _formatMap = editorFormatMapService.GetEditorFormatMap("text");
            _formatMap.FormatMappingChanged += OnFormatMappingChanged;
        }

        private void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            if (e.ChangedItems.Contains(GuidelineColorDefinition.c_name))
            {
                _brush = GetGuidelineBrushFromFontsAndColors();
                BrushChanged?.Invoke(this, _brush);
            }
        }

        public Brush Brush => _brush ?? (_brush = GetGuidelineBrushFromFontsAndColors());

        public event EventHandler<Brush> BrushChanged;

        private Brush GetGuidelineBrushFromFontsAndColors()
        {
            var resourceDictionary = _formatMap.GetProperties(GuidelineColorDefinition.c_name);
            if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundBrushId))
            {
                return resourceDictionary[EditorFormatDefinition.BackgroundBrushId] as Brush;
            }

            return null;
        }
    }
}
