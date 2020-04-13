using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WindowTrackerApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string databaseName = "WindowTrackerApp.db";
        static string folderPath = AppDomain.CurrentDomain.BaseDirectory;
        public static string databasePath = System.IO.Path.Combine(folderPath, databaseName);

        protected override void OnStartup(StartupEventArgs e)
        {
            //XamlDisplay.Init();
            base.OnStartup(e);

            MainWindow app = new MainWindow();
            MainViewModel context = new MainViewModel();

            app.DataContext = context;
            app.Show();

            app.Closing += context.OnWindowClosing;
        }
    }
}
