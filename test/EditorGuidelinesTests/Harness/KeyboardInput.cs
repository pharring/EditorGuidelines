// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using WindowsInput.Native;

namespace EditorGuidelinesTests.Harness
{
    public readonly struct KeyboardInput
    {
        internal readonly object Value;

        private KeyboardInput(string value)
        {
            Value = value;
        }

        private KeyboardInput(char value)
        {
            Value = value;
        }

        private KeyboardInput(VirtualKeyCode value)
        {
            Value = value;
        }

        private KeyboardInput((VirtualKeyCode virtualKeyCode, ShiftState shiftState) value)
        {
            Value = value;
        }

        public static implicit operator KeyboardInput(string value) => new KeyboardInput(value);
        public static implicit operator KeyboardInput(char value) => new KeyboardInput(value);
        public static implicit operator KeyboardInput(VirtualKeyCode value) => new KeyboardInput(value);
        public static implicit operator KeyboardInput((VirtualKeyCode virtualKeyCode, ShiftState shiftState) value) => new KeyboardInput(value);
    }
}
