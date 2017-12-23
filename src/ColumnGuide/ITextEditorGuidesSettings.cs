using System.Collections.Generic;

namespace ColumnGuide
{
    interface ITextEditorGuidesSettings
    {
        IEnumerable<int> GuideLinePositionsInChars { get; }
    }
}
