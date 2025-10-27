using System;
using System.IO;
using System.Windows;
using menza_admin.Services;
using Microsoft.Extensions.Configuration;

namespace menza_admin
{
    public partial class App : Application
    {
        public static api Api;

        protected override void OnStartup(StartupEventArgs e)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            var config = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var baseUrl = config["ApiBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                MessageBox.Show("ApiBaseUrl not configured in appsettings.json", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            Api = new api(baseUrl);

            base.OnStartup(e);
        }
    }
}
