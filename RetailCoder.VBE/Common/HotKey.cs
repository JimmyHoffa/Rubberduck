using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Rubberduck.Common.WinAPI;

namespace Rubberduck.Common
{
    public class HotKey : IHotKey
    {
        private readonly string _key;
        private readonly IntPtr _hWndVbe;

        public HotKey(IntPtr hWndVbe, string key, Keys secondKey = Keys.None)
        {
            _hWndVbe = hWndVbe;

            IsTwoStepHotKey = secondKey != Keys.None;
            _key = key;
            Combo = GetCombo(key);
            SecondKey = secondKey;
        }

        public HotKeyInfo HotKeyInfo { get; private set; }
        public Keys Combo { get; private set; }
        public Keys SecondKey { get; private set; }
        public bool IsTwoStepHotKey { get; private set; }
        public bool IsAttached { get; private set; }

        public void Attach()
        {
            var hotKey = _key;
            var shift = GetModifierValue(ref hotKey);
            var key = GetKey(hotKey);

            if (key == Keys.None)
            {
                throw new InvalidOperationException("Invalid key.");
            }

            HookKey(key, shift);
        }

        public void Detach()
        {
            if (!IsAttached)
            {
                throw new InvalidOperationException("Hook is already detached.");
            }

            User32.UnregisterHotKey(_hWndVbe, HotKeyInfo.HookId);
            Kernel32.GlobalDeleteAtom(HotKeyInfo.HookId);

            IsAttached = false;
        }

        private void HookKey(Keys key, uint shift)
        {
            if (IsAttached)
            {
                throw new InvalidOperationException("Hook is already attached.");
            }

            var hookId = (IntPtr)Kernel32.GlobalAddAtom(Guid.NewGuid().ToString());
            var success = User32.RegisterHotKey(_hWndVbe, hookId, shift, (uint)key);
            if (!success)
            {
                throw new Win32Exception("HotKey was not registered.");
            }

            HotKeyInfo = new HotKeyInfo(hookId, Combo);
            IsAttached = true;
        }

        private static readonly IDictionary<char,uint> Modifiers = new Dictionary<char, uint>
        {
            { '+', (uint)KeyModifier.SHIFT },
            { '%', (uint)KeyModifier.ALT },
            { '^', (uint)KeyModifier.CONTROL },
        };

        /// <summary>
        /// Gets the <see cref="KeyModifier"/> values out of a key combination.
        /// </summary>
        /// <param name="key">The hotkey string, returned without the modifiers.</param>
        private static uint GetModifierValue(ref string key)
        {
            uint result = 0;

            for (var i = 0; i < 3; i++)
            {
                var firstChar = key[0];
                if (Modifiers.ContainsKey(firstChar))
                {
                    result |= Modifiers[firstChar];
                }
                else
                {
                    // first character isn't a modifier symbol:
                    break;
                }

                // truncate first character for next iteration:
                key = key.Substring(1);
            }

            return result;
        }

        private static Keys GetCombo(string key)
        {
            var combo = key;
            var modifiers = GetModifierValue(ref combo);
            return (Keys)Enum.Parse(typeof(Keys), combo) // will break with special keys, e.g. {f12}
                   | (key.Contains("%") ? Keys.Alt : Keys.None)
                   | (key.Contains("^") ? Keys.Control : Keys.None)
                   | (key.Contains("+") ? Keys.Shift : Keys.None);
        }

        private static Keys GetKey(string keyCode)
        {
            var result = Keys.None;
            switch (keyCode.Substring(0, 1))
            {
                case "{":
                    _keys.TryGetValue(keyCode, out result);
                    break;
                case "~":
                    result = Keys.Return;
                    break;
                default:
                    if (!String.IsNullOrEmpty(keyCode))
                    {
                        result = (Keys)Enum.Parse(typeof(Keys), keyCode);
                    }
                    break;
            }

            return result;
        }

        private static readonly IDictionary<string, Keys> _keys = new Dictionary<string, Keys>
        {
            { "{BACKSPACE}", Keys.Back },
            { "{BS}", Keys.Back },
            { "{BKSP}", Keys.Back },
            { "{CAPSLOCK}", Keys.CapsLock },
            { "{DELETE}", Keys.Delete },
            { "{DEL}", Keys.Delete },
            { "{DOWN}", Keys.Down },
            { "{END}", Keys.End },
            { "{ENTER}", Keys.Enter },
            { "{RETURN}", Keys.Enter },
            { "{ESC}", Keys.Escape },
            { "{HELP}", Keys.Help },
            { "{HOME}", Keys.Home },
            { "{INSERT}", Keys.Insert },
            { "{INS}", Keys.Insert },
            { "{LEFT}", Keys.Left },
            { "{NUMLOCK}", Keys.NumLock },
            { "{PGDN}", Keys.PageDown },
            { "{PGUP}", Keys.PageUp },
            { "{PRTSC}", Keys.PrintScreen },
            { "{RIGHT}", Keys.Right },
            { "{TAB}", Keys.Tab },
            { "{UP}", Keys.Up },
            { "{F1}", Keys.F1 },
            { "{F2}", Keys.F2 },
            { "{F3}", Keys.F3 },
            { "{F4}", Keys.F4 },
            { "{F5}", Keys.F5 },
            { "{F6}", Keys.F6 },
            { "{F7}", Keys.F7 },
            { "{F8}", Keys.F8 },
            { "{F9}", Keys.F9 },
            { "{F10}", Keys.F10 },
            { "{F11}", Keys.F11 },
            { "{F12}", Keys.F12 },
            { "{F13}", Keys.F13 },
            { "{F14}", Keys.F14 },
            { "{F15}", Keys.F15 },
            { "{F16}", Keys.F16 },
        };
    }
}