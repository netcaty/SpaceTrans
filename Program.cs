using System;
using System.IO;
using System.Windows.Forms;

namespace SpaceTrans
{
    internal class Program
    {
        #if TRAY
        [STAThread]
        #endif
        static void Main(string[] args)
        {
            #if TRAY
            if (args.Contains("--tray"))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplication());
                return;
            }
            // ...其它启动逻辑...
            #endif
            
            #if CLI
            RunConsoleMode();
            #elif TRAY
            RunTrayMode();
            #else
             #error "Please select a mode"
            #endif
        }

        #if CLI
        private static void RunConsoleMode()
        {
            using var consoleApp = new ConsoleApplication();
            consoleApp.Run();
        }
        #endif

        #if TRAY
        private static void RunTrayMode()
        {
            Application.Run(new TrayApplication());
        }
        #endif
    }
}
