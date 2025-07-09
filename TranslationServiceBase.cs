using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using SpaceTrans.Engines;

namespace SpaceTrans
{
    public abstract class TranslationServiceBase : IDisposable
    {
        protected const int WH_KEYBOARD_LL = 13;
        protected const int WM_KEYDOWN = 0x0100;
        protected const int VK_SPACE = 0x20;

        protected static DateTime lastSpaceTime = DateTime.MinValue;
        protected static LowLevelKeyboardProc _proc;
        protected static IntPtr _hookID = IntPtr.Zero;
        protected static readonly HttpClient httpClient = new();
        protected static TranslationEngineManager engineManager;
        public static ConfigManager configManager;

        #if CLI
        protected static readonly IPlatformService platformService = PlatformServiceFactory.Create();
        #endif

        public bool hotkeyEnabled = true;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr GetModuleHandle(string lpModuleName);

        public virtual void InitializeEngines()
        {
            Logger.Instance?.Debug("Initializing translation engines...");
            
            configManager = new ConfigManager();
            engineManager = new TranslationEngineManager();
            
            var config = configManager.GetConfig();
            
            // Register Youdao engine
            if (!string.IsNullOrEmpty(config.YoudaoConfig.AppKey))
            {
                var youdaoEngine = new YoudaoTranslationEngine(
                    config.YoudaoConfig.AppKey,
                    config.YoudaoConfig.AppSecret,
                    httpClient);
                engineManager.RegisterEngine(youdaoEngine);
                Logger.Instance?.LogEngineEvent("Youdao", "registered");
            }
            
            // Register Gemini engine if API key is provided
            if (!string.IsNullOrEmpty(config.GeminiConfig.ApiKey))
            {
                var geminiEngine = new GeminiTranslationEngine(
                    config.GeminiConfig.ApiKey,
                    httpClient);
                engineManager.RegisterEngine(geminiEngine);
                Logger.Instance?.LogEngineEvent("Gemini", "registered");
            }
            
            // Set current engine
            try
            {
                engineManager.SetCurrentEngine(config.CurrentEngine);
                Logger.Instance?.LogEngineEvent(config.CurrentEngine, "set as current");
            }
            catch
            {
                Logger.Instance?.Warning($"Engine '{config.CurrentEngine}' not available, using default");
                OnEngineError($"Engine '{config.CurrentEngine}' not available, using default.");
            }
        }

        public virtual void SetupHotkey()
        {
            Logger.Instance?.Debug("Setting up global hotkey...");
            
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            
            if (_hookID == IntPtr.Zero)
            {
                Logger.Instance?.Error("Failed to install global hotkey");
                OnHotkeyError("Failed to install global hotkey");
                hotkeyEnabled = false;
            }
            else
            {
                Logger.Instance?.LogHotkeyEvent("Global hotkey installed successfully");
            }
        }

        protected static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        protected IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // 快速返回，减少钩子处理时间
            if (!hotkeyEnabled || nCode < 0 || wParam != (IntPtr)WM_KEYDOWN)
            {
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            var vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == VK_SPACE)
            {
                // 使用高精度时间戳并在后台线程中处理
                var currentTime = DateTime.UtcNow;
                var timeDiff = currentTime - lastSpaceTime;

                if (timeDiff.TotalMilliseconds < 500 && timeDiff.TotalMilliseconds > 50)
                {
                    // 异步处理，不阻塞钩子
                    Task.Run(async () =>
                    {
                        try
                        {
                            Logger.Instance?.LogHotkeyEvent("Double space detected, triggering translation");
                            await ProcessDoubleSpace();
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance?.Error("Error in ProcessDoubleSpace", ex);
                        }
                    });
                }

                lastSpaceTime = currentTime;
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        protected async virtual Task ProcessDoubleSpace()
        {
            try
            {
                var content = await GetActiveInputText();
                if (string.IsNullOrWhiteSpace(content))
                {
                    Logger.Instance?.Debug("No content selected for translation");
                    return;
                }

                Logger.Instance?.LogTranslationStart(content);
                
                var config = configManager.GetConfig();
                var currentEngine = engineManager.GetCurrentEngine();
                var translatedContent = await engineManager.TranslateAsync(content, "auto", config.TargetLanguage);
                
                await SetClipboardTextWithRetry(translatedContent);
                await SendKeysWithDelay("^a", 50);
                await SendKeysWithDelay("^v", 50);
                
                Logger.Instance?.LogTranslationSuccess(content, translatedContent, currentEngine.Name);
                OnTranslationSuccess($"Translated to {config.TargetLanguage}");
            }
            catch (Exception ex)
            {
                Logger.Instance?.Error($"Translation failed during double space processing", ex);
                OnTranslationError($"Translation failed: {ex.Message}");
            }
        }


        // Helper methods for clipboard and keyboard operations
        protected static async Task<string> GetActiveInputText()
        {
            try
            {
                var originalClipboard = await GetClipboardTextWithRetry();
                
                await SendKeysWithDelay("^a", 30);
                await SendKeysWithDelay("^c", 50);
                
                var content = await GetClipboardTextWithRetry();
                
                await SetClipboardTextWithRetry(originalClipboard);
                
                return content;
            }
            catch (Exception)
            {
                return "";
            }
        }

        protected static async Task SendKeysWithDelay(string keys, int delayMs)
        {
#if CLI
            await platformService.SendKeysAsync(keys);
#else
            SendKeys.SendWait(keys);
#endif
            await Task.Delay(delayMs);
        }

        protected static async Task<string> GetClipboardTextWithRetry(int retry = 3)
        {
            for (int i = 0; i < retry; i++)
            {
                try
                {
                    return await GetClipboardText();
                }
                catch
                {
                    await Task.Delay(50);
                }
            }
            return "";
        }

        protected static async Task SetClipboardTextWithRetry(string text, int retry = 3)
        {
            for (int i = 0; i < retry; i++)
            {
                try
                {
                    await SetClipboardText(text);
                    return;
                }
                catch
                {
                    await Task.Delay(50);
                }
            }
        }

        protected static async Task<string> GetClipboardText()
        {
#if CLI
            return await platformService.GetClipboardTextAsync();
#else
            var tcs = new TaskCompletionSource<string>();

            var thread = new Thread(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        tcs.SetResult(Clipboard.GetText());
                    }
                    else
                    {
                        tcs.SetResult("");
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return await tcs.Task;
#endif
        }

        protected static async Task SetClipboardText(string text)
        {
#if CLI
            await platformService.SetClipboardTextAsync(text);
#else
            var tcs = new TaskCompletionSource<bool>();

            var thread = new Thread(() =>
            {
                try
                {
                    Clipboard.SetText(text);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            await tcs.Task;
#endif
        }

        // Abstract methods for different UI implementations
        protected abstract void OnTranslationSuccess(string message);
        protected abstract void OnTranslationError(string message);
        protected abstract void OnTranslationWarning(string message);
        protected abstract void OnEngineError(string message);
        protected abstract void OnHotkeyError(string message);

        protected virtual void Cleanup()
        {
            Logger.Instance?.Debug("Cleaning up translation service resources");
            
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                Logger.Instance?.LogHotkeyEvent("Global hotkey uninstalled");
            }
        }

        public virtual void Dispose()
        {
            Cleanup();
            httpClient?.Dispose();
            Logger.Instance?.Debug("Translation service disposed");
        }
    }
}