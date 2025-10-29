using System;
using System.IO;
using System.Windows;
using menza_admin.Services;
using Microsoft.Extensions.Configuration;

namespace menza_admin
{
    public partial class App : Application
    {
        public static Api Api { get; private set; }
        private static IConfiguration Configuration { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                InitializeConfiguration();
                InitializeApi();
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Application initialization failed:\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void InitializeConfiguration()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            Configuration = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .Build();
        }

        private void InitializeApi()
        {
            var baseUrl = Configuration["ApiBaseUrl"];
            
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "ApiBaseUrl not configured in appsettings.json. Please check your configuration.");
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    $"Invalid ApiBaseUrl format: {baseUrl}. URL must be absolute (e.g., http://localhost:3001)");
            }

            Api = new Api(baseUrl);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup if needed
            Api?.Dispose();  // Note: You'll need to implement IDisposable in api class
            base.OnExit(e);
        }
    }
}
