// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace ColumnGuide
{
    internal interface ITextEditorGuidesSettings
    {
        IEnumerable<int> GuideLinePositionsInChars { get; }
    }
}
