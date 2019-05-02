// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;

namespace EditorGuidelinesTests.Harness
{
    [Flags]
    public enum ShiftState
    {
        None = 0,
        Shift = 1 << 0,
        Ctrl = 1 << 1,
        Alt = 1 << 2,
        Hankaku = 1 << 3,
        Reserved1 = 1 << 4,
        Reserved2 = 1 << 5,
    }
}
