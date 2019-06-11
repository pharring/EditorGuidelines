// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace EditorGuidelinesTests.Harness
{
    internal sealed class SendInputServices : AbstractServices
    {
        public SendInputServices(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task SendAsync(params KeyboardInput[] inputs)
        {
            await SendAsync(inputSimulator =>
            {
                foreach (var input in inputs)
                {
                    SendInput(inputSimulator, input);
                }
            });
        }

        private void SendInput(InputSimulator inputSimulator, KeyboardInput input)
        {
            switch (input.Value)
            {
                case string stringValue:
                    var text = stringValue.Replace("\r\n", "\r").Replace('\n', '\r');
                    foreach (var c in text)
                    {
                        SendInput(inputSimulator, c);
                    }

                    break;

                case char charValue:
                    SendCharacter(inputSimulator, charValue);
                    break;

                case VirtualKeyCode virtualKeyCode:
                    SendVirtualKey(inputSimulator, virtualKeyCode);
                    break;

                case ValueTuple<VirtualKeyCode, ShiftState> modifiedVirtualKey:
                    SendVirtualKey(inputSimulator, modifiedVirtualKey.Item1, modifiedVirtualKey.Item2);
                    break;

                case null:
                    throw new ArgumentNullException(nameof(input));

                default:
                    throw new InvalidOperationException("Not reachable");
            }
        }

        private void SendCharacter(InputSimulator inputSimulator, char ch)
        {
            var keyCode = NativeMethods.VkKeyScan(ch);
            if (keyCode == -1)
            {
                // This is a Unicode character, or otherwise cannot be mapped to a virtual key code
                SendUnicodeCharacter(inputSimulator, ch);
                return;
            }

            var virtualKey = (VirtualKeyCode)(keyCode & 0xFF);
            var shiftState = (ShiftState)(((ushort)keyCode >> 8) & 0xFF);
            SendVirtualKey(inputSimulator, virtualKey, shiftState);
        }

        private void SendUnicodeCharacter(InputSimulator inputSimulator, char ch)
        {
            inputSimulator.Keyboard.TextEntry(ch);
        }

        private void SendVirtualKey(InputSimulator inputSimulator, VirtualKeyCode virtualKey, ShiftState shiftState = ShiftState.None)
        {
            var modifiers = new List<VirtualKeyCode>();
            if (shiftState.HasFlag(ShiftState.Shift))
            {
                modifiers.Add(VirtualKeyCode.SHIFT);
            }

            if (shiftState.HasFlag(ShiftState.Ctrl))
            {
                modifiers.Add(VirtualKeyCode.CONTROL);
            }

            if (shiftState.HasFlag(ShiftState.Alt))
            {
                modifiers.Add(VirtualKeyCode.MENU);
            }

            inputSimulator.Keyboard.ModifiedKeyStroke(modifiers, virtualKey);
        }

        private async Task SendAsync(Action<InputSimulator> action)
        {
            await TestServices.VisualStudio.ActivateMainWindowAsync();
            await Task.Run(() => action(new InputSimulator()));
            await Task.Yield();
        }
    }
}
