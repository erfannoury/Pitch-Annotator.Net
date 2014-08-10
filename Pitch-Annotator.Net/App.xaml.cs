using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace PitchAnnotator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>c
    public partial class App : Application
    {
        public App()
        {
            TextWriterTraceListener textWriteTraceListener = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(textWriteTraceListener);
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = e.Args;
            foreach(var arg in args)
            {
                Console.WriteLine(arg);
            }
            if(args.Length != 2)
            {
                Console.WriteLine("You should enter two arguments: ");
                Console.WriteLine("Pitch-Annotator.Net.exe path/to/images/folder path/to/annotations/folder");
                return;
            }
            else
            {
                MainWindow wnd = new MainWindow(args[0], args[1]);
                wnd.Show();
            }
        }
    }
}
