using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpaceTrans
{
    // Platform-specific services interface
    public interface IPlatformService
    {
        Task<string> GetClipboardTextAsync();
        Task SetClipboardTextAsync(string text);
        Task SendKeysAsync(string keys);
    }

    // Windows-specific platform service using Win32 API
    public class WindowsPlatformService : IPlatformService
    {
        // Clipboard Win32 API declarations
        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern UIntPtr GlobalSize(IntPtr hMem);

        // SendInput Win32 API declarations
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        // Constants
        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        // Input structure for SendInput
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        public async Task<string> GetClipboardTextAsync()
        {
            // 使用专用线程避免阻塞
            return await Task.Run(() =>
            {
                // 减少重试次数，提高响应速度
                for (int retry = 0; retry < 2; retry++)
                {
                    try
                    {
                        if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
                            return string.Empty;

                        if (!OpenClipboard(IntPtr.Zero))
                        {
                            if (retry == 0) Thread.Sleep(10); // 减少等待时间
                            continue;
                        }

                        try
                        {
                            IntPtr hData = GetClipboardData(CF_UNICODETEXT);
                            if (hData == IntPtr.Zero)
                                return string.Empty;

                            IntPtr pData = GlobalLock(hData);
                            if (pData == IntPtr.Zero)
                                return string.Empty;

                            try
                            {
                                string text = Marshal.PtrToStringUni(pData) ?? string.Empty;
                                return text;
                            }
                            finally
                            {
                                GlobalUnlock(hData);
                            }
                        }
                        finally
                        {
                            CloseClipboard();
                        }
                    }
                    catch
                    {
                        if (retry == 0) Thread.Sleep(10);
                    }
                }
                return string.Empty;
            });
        }

        public async Task SetClipboardTextAsync(string text)
        {
            await Task.Run(() =>
            {
                // 减少重试，提高响应速度
                for (int retry = 0; retry < 2; retry++)
                {
                    try
                    {
                        if (!OpenClipboard(IntPtr.Zero))
                        {
                            if (retry == 0) Thread.Sleep(10);
                            continue;
                        }

                        try
                        {
                            EmptyClipboard();

                            if (string.IsNullOrEmpty(text))
                                return;

                            byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");
                            IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
                            if (hMem == IntPtr.Zero)
                                return;

                            IntPtr pMem = GlobalLock(hMem);
                            if (pMem == IntPtr.Zero)
                                return;

                            try
                            {
                                Marshal.Copy(bytes, 0, pMem, bytes.Length);
                                SetClipboardData(CF_UNICODETEXT, hMem);
                                return; // 成功则立即返回
                            }
                            finally
                            {
                                GlobalUnlock(hMem);
                            }
                        }
                        finally
                        {
                            CloseClipboard();
                        }
                    }
                    catch
                    {
                        if (retry == 0) Thread.Sleep(10);
                    }
                }
            });
        }

        public async Task SendKeysAsync(string keys)
        {
            await Task.Run(() =>
            {
                try
                {
                    // 处理Ctrl组合键
                    switch (keys.ToLower())
                    {
                        case "^a":
                            SendCtrlA();
                            break;
                        case "^c":
                            SendCtrlC();
                            break;
                        case "^v":
                            SendCtrlV();
                            break;
                        default:
                            // 对于其他键，逐个字符发送
                            foreach (char c in keys)
                            {
                                if (c != '^') // 跳过控制字符
                                {
                                    SendUnicodeChar(c);
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance?.Error($"SendKeys error for '{keys}'", ex);
                }
            });
        }

        private void SendCtrlA()
        {
            SendCtrlKey(0x41); // VK_A
        }

        private void SendCtrlC()
        {
            SendCtrlKey(0x43); // VK_C
        }

        private void SendCtrlV()
        {
            SendCtrlKey(0x56); // VK_V
        }

        private void SendCtrlKey(ushort virtualKey)
        {
            INPUT[] inputs = new INPUT[4];

            // Press Ctrl
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0x11, // VK_CONTROL
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Press target key
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Release target key
            inputs[2] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Release Ctrl
            inputs[3] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0x11, // VK_CONTROL
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SendUnicodeChar(char c)
        {
            INPUT[] inputs = new INPUT[2];

            // Key down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }

    // Factory for creating platform services
    public static class PlatformServiceFactory
    {
        public static IPlatformService Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsPlatformService();
            }
            else
            {
                throw new PlatformNotSupportedException("Only Windows is currently supported");
            }
        }
    }
}