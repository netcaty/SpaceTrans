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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
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
