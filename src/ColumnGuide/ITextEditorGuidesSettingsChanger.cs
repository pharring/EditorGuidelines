using System;

namespace ColumnGuide
{
    public interface ITextEditorGuidesSettingsChanger
    {
        bool AddGuideline(int column);
        bool RemoveGuideline(int column);
        bool CanAddGuideline(int column);
        bool CanRemoveGuideline(int column);
        void RemoveAllGuidelines();
    }
}
