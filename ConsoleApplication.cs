using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpaceTrans
{
    public class ConsoleApplication : TranslationServiceBase
    {
        private CancellationTokenSource cancellationTokenSource;

        public void Run()
        {
            Console.WriteLine("SpaceTrans started in console mode. Press double space to translate clipboard content.");
            Console.WriteLine("Press Ctrl+C to exit.");

            Logger.Initialize("", LogLevel.Debug, LogOutput.Console);
            Logger.Instance?.Info("SpaceTrans CLI application starting...");
            InitializeEngines();
            SetupHotkey();

            cancellationTokenSource = new CancellationTokenSource();
            
            // 设置Ctrl+C处理器
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true; // 阻止默认的强制退出
                Logger.Instance?.Info("Ctrl+C pressed, initiating graceful shutdown...");
                Console.WriteLine("\nShutting down gracefully...");
                cancellationTokenSource.Cancel();
                
                // 退出所有消息循环
                Application.Exit();
            };

            Logger.Instance?.Debug("Starting Windows message loop for keyboard hook...");
            
            try
            {
                // 主线程运行消息循环 - 这对全局键盘钩子至关重要
                Application.Run();
            }
            catch (Exception ex)
            {
                Logger.Instance?.Error("Message loop error", ex);
            }
            finally
            {
                Logger.Instance?.Info("Shutting down application...");
                Cleanup();
                
                Console.WriteLine("Application closed.");
            }
        }

        protected override void OnTranslationSuccess(string message)
        {
            var coloredMessage = $"[{DateTime.Now:HH:mm:ss}] ✅ {message}";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(coloredMessage);
            Console.ResetColor();
            Logger.Instance?.Info($"Translation success: {message}");
        }

        protected override void OnTranslationError(string message)
        {
            var coloredMessage = $"[{DateTime.Now:HH:mm:ss}] ❌ Error: {message}";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(coloredMessage);
            Console.ResetColor();
            Logger.Instance?.Error($"Translation error: {message}");
        }

        protected override void OnTranslationWarning(string message)
        {
            var coloredMessage = $"[{DateTime.Now:HH:mm:ss}] ⚠️ Warning: {message}";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(coloredMessage);
            Console.ResetColor();
            Logger.Instance?.Warning($"Translation warning: {message}");
        }

        protected override void OnEngineError(string message)
        {
            var coloredMessage = $"[{DateTime.Now:HH:mm:ss}] 🔧 Engine Error: {message}";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(coloredMessage);
            Console.ResetColor();
            Logger.Instance?.Error($"Engine error: {message}");
        }

        protected override void OnHotkeyError(string message)
        {
            var coloredMessage = $"[{DateTime.Now:HH:mm:ss}] ⌨️ Hotkey Error: {message}";
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(coloredMessage);
            Console.ResetColor();
            Logger.Instance?.Error($"Hotkey error: {message}");
        }

        public override void Dispose()
        {
            cancellationTokenSource?.Dispose();
            base.Dispose();
        }
    }
}