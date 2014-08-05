using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Pitch_Annotator.Net
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            TextWriterTraceListener textWriteTraceListener = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(textWriteTraceListener);
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow wind = new MainWindow();
            wind.Show();
        }
    }
}
