using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SpaceTrans.Engines;

namespace SpaceTrans
{
    public class TrayApplication : ApplicationContext
    {
        public static NotifyIcon GlobalTrayIcon; // 新增

        private TranslationServiceBase translationService;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public TrayApplication()
        {
            // Initialize logger for tray mode
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpaceTrans", "app.log");
            Logger.Initialize(logPath, LogLevel.Info);
            Logger.Instance.Info("SpaceTrans tray application starting...");

            // Rotate log file if it's getting too large
            Logger.Instance.RotateLogIfNeeded();

            translationService = new TrayTranslationService(this);
            InitializeComponent();
            translationService.InitializeEngines();
            translationService.SetupHotkey();

            Logger.Instance.Info("SpaceTrans tray application started successfully");

            GlobalTrayIcon = trayIcon; // 新增
        }

        private void InitializeComponent()
        {
            // 创建托盘图标
            trayIcon = new NotifyIcon()
            {
                Icon = GetTrayIcon(),
                Text = "SpaceTrans",
                Visible = true
            };

            // 创建右键菜单
            trayMenu = new ContextMenuStrip();

            var openLogMenuItem = new ToolStripMenuItem("Open Log File", null, OnOpenLogFile);
            var toggleHotkeyMenuItem = new ToolStripMenuItem("Enable Hotkey", null, OnToggleHotkey)
            {
                Checked = translationService.hotkeyEnabled
            };
            var settingsMenuItem = new ToolStripMenuItem("Settings...", null, OnSettings);
            var aboutMenuItem = new ToolStripMenuItem("About", null, OnAbout);
            var exitMenuItem = new ToolStripMenuItem("Exit", null, OnExit);

            trayMenu.Items.AddRange(new ToolStripItem[] {
                openLogMenuItem,
                new ToolStripSeparator(),
                toggleHotkeyMenuItem,
                settingsMenuItem,
                new ToolStripSeparator(),
                aboutMenuItem,
                exitMenuItem
            });

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += OnSettings;
        }

        private void OnOpenLogFile(object sender, EventArgs e)
        {
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpaceTrans", "app.log");

                if (File.Exists(logPath))
                {
                    // Try to open with default text editor
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logPath,
                        UseShellExecute = true
                    });

                    Logger.Instance.Info("Log file opened by user");
                }
                else
                {
                    // Create empty log file and open it
                    Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                    File.WriteAllText(logPath, "SpaceTrans log file\n");

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logPath,
                        UseShellExecute = true
                    });

                    Logger.Instance.Info("Empty log file created and opened");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Failed to open log file", ex);

                // Fallback: try to open the log directory
                try
                {
                    var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpaceTrans");
                    Directory.CreateDirectory(logDir);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logDir,
                        UseShellExecute = true
                    });

                    Logger.Instance.Info("Log directory opened instead");
                }
                catch
                {
                    // Show error if everything fails
                    MessageBox.Show($"Could not open log file. Log location:\n{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpaceTrans", "app.log")}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnToggleHotkey(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            translationService.hotkeyEnabled = !translationService.hotkeyEnabled;
            menuItem.Checked = translationService.hotkeyEnabled;

            Logger.Instance.Info($"Hotkey {(translationService.hotkeyEnabled ? "enabled" : "disabled")} via tray menu");
        }

        private void OnSettings(object sender, EventArgs e)
        {
            Logger.Instance.Info("Opening settings dialog");
            var settingsForm = new SettingsForm(TranslationServiceBase.configManager);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                // Reload engines with new configuration
                translationService.InitializeEngines();
                Logger.Instance.Info("Settings updated and engines reloaded");
            }
            else
            {
                Logger.Instance.Info("Settings dialog cancelled");
            }
        }

        private void OnAbout(object sender, EventArgs e)
        {
            MessageBox.Show(
                "SpaceTrans v1.0\n\nA fast translation tool with global hotkey support.\n\nTriple-press Space to translate text when you type anywhere.\n\nRight-click tray icon to access log file and settings.",
                "About SpaceTrans",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Logger.Instance.Info("Exiting SpaceTrans application");
            translationService?.Dispose();
            trayIcon?.Dispose();
            Application.Exit();
        }

        internal void LogEvent(string message, LogLevel level = LogLevel.Info)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    Logger.Instance.Debug(message);
                    break;
                case LogLevel.Info:
                    Logger.Instance.Info(message);
                    break;
                case LogLevel.Warning:
                    Logger.Instance.Warning(message);
                    break;
                case LogLevel.Error:
                    Logger.Instance.Error(message);
                    break;
            }
        }

        private Icon GetTrayIcon()
        {
            try
            {
                // 1. 尝试从应用目录加载自定义图标
                string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }

                // 2. 尝试从嵌入资源加载图标
                var assembly = Assembly.GetExecutingAssembly();
                var iconStream = assembly.GetManifestResourceStream("SpaceTrans.Resources.icon.ico");
                if (iconStream != null)
                {
                    return new Icon(iconStream);
                }

                // 3. 创建简单的文字图标
                return CreateTextIcon("译", Color.DodgerBlue, Color.White);
            }
            catch
            {
                // 4. 回退到系统默认图标
                return SystemIcons.Application;
            }
        }

        private Icon CreateTextIcon(string text, Color backgroundColor, Color textColor)
        {
            // 根据系统DPI设置确定图标尺寸
            int iconSize = GetSystemTrayIconSize();

            using var bitmap = new Bitmap(iconSize, iconSize);
            using var graphics = Graphics.FromImage(bitmap);

            // 启用抗锯齿
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 设置背景色
            graphics.Clear(backgroundColor);

            // 动态计算字体大小
            float fontSize = iconSize * 0.6f; // 图标尺寸的60%
            using var font = new Font("Microsoft YaHei", fontSize, FontStyle.Bold);
            using var brush = new SolidBrush(textColor);

            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            graphics.DrawString(text, font, brush, new RectangleF(0, 0, iconSize, iconSize), stringFormat);

            // 转换为图标
            IntPtr hIcon = bitmap.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private int GetSystemTrayIconSize()
        {
            try
            {
                // 获取系统DPI
                using var graphics = Graphics.FromHwnd(IntPtr.Zero);
                float dpiX = graphics.DpiX;

                // 根据DPI计算合适的图标尺寸
                if (dpiX <= 96) return 16;  // 100% DPI
                if (dpiX <= 120) return 20;  // 125% DPI  
                if (dpiX <= 144) return 24;  // 150% DPI
                return 32;                   // 200%+ DPI
            }
            catch
            {
                return 16; // 默认尺寸
            }
        }

        public void UpdateTrayIcon(string status = "normal")
        {
            if (trayIcon == null) return;

            try
            {
                switch (status.ToLower())
                {
                    case "translating":
                        trayIcon.Icon = CreateTextIcon("译", Color.Orange, Color.White);
                        trayIcon.Text = "SpaceTrans - Translating...";
                        break;
                    case "success":
                        trayIcon.Icon = CreateTextIcon("✓", Color.Green, Color.White);
                        trayIcon.Text = "SpaceTrans - Translation Complete";
                        // 2秒后恢复正常图标
                        var timer = new System.Threading.Timer(_ =>
                        {
                            if (trayIcon != null)
                            {
                                trayIcon.Icon = GetTrayIcon();
                                trayIcon.Text = "SpaceTrans";
                            }
                        }, null, 2000, System.Threading.Timeout.Infinite);
                        break;
                    case "error":
                        trayIcon.Icon = CreateTextIcon("✗", Color.Red, Color.White);
                        trayIcon.Text = "SpaceTrans - Translation Failed";
                        // 3秒后恢复正常图标
                        var errorTimer = new System.Threading.Timer(_ =>
                        {
                            if (trayIcon != null)
                            {
                                trayIcon.Icon = GetTrayIcon();
                                trayIcon.Text = "SpaceTrans";
                            }
                        }, null, 3000, System.Threading.Timeout.Infinite);
                        break;
                    default:
                        trayIcon.Icon = GetTrayIcon();
                        trayIcon.Text = "SpaceTrans";
                        break;
                }
            }
            catch
            {
                // 如果更新失败，使用默认图标
                trayIcon.Icon = SystemIcons.Application;
                trayIcon.Text = "SpaceTrans";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                translationService?.Dispose();
                trayIcon?.Dispose();
            }
            base.Dispose(disposing);
        }

        private class TrayTranslationService : TranslationServiceBase
        {
            private TrayApplication parent;

            public TrayTranslationService(TrayApplication parentApp)
            {
                parent = parentApp;
            }

            protected override void OnTranslationSuccess(string message)
            {
                parent?.UpdateTrayIcon("success");
                parent?.LogEvent($"Translation successful: {message}", LogLevel.Info);
            }

            protected override void OnTranslationError(string message)
            {
                parent?.UpdateTrayIcon("error");
                parent?.LogEvent($"Translation error: {message}", LogLevel.Error);
            }

            protected override void OnTranslationWarning(string message)
            {
                parent?.LogEvent($"Translation warning: {message}", LogLevel.Warning);
            }

            protected override void OnEngineError(string message)
            {
                parent?.LogEvent($"Engine error: {message}", LogLevel.Warning);
            }

            protected override void OnHotkeyError(string message)
            {
                parent?.LogEvent($"Hotkey error: {message}", LogLevel.Error);
            }

            protected override async Task ProcessDoubleSpaceOptimized()
            {
                parent?.UpdateTrayIcon("translating");
                await base.ProcessDoubleSpaceOptimized();
            }
        }

        public static void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (GlobalTrayIcon != null)
            {
                GlobalTrayIcon.BalloonTipTitle = title;
                GlobalTrayIcon.BalloonTipText = text;
                GlobalTrayIcon.BalloonTipIcon = icon;
                GlobalTrayIcon.ShowBalloonTip(3000);
            }
        }
    }
}