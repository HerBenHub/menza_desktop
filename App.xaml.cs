using System;
using System.Windows;
using menza_admin.Services;
using Microsoft.Extensions.Configuration;

namespace menza_admin
{
    /// <summary>
    /// Alkalmazás osztály - az alkalmazás belépési pontja és globális erőforrás kezelője
    /// </summary>
    public partial class App : Application
    {
        public static Api Api { get; private set; } // Globális API kliens példány
        private static IConfiguration Configuration { get; set; } // Konfigurációs beállítások

        /// <summary>
        /// Alkalmazás indításakor fut le
        /// Inicializálja a konfigurációt és az API klienst
        /// </summary>
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
                    $"Az alkalmazás inicializálása sikertelen:\n{ex.Message}",
                    "Indítási hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        /// <summary>
        /// Betölti az alkalmazás konfigurációját az appsettings.json fájlból
        /// </summary>
        private void InitializeConfiguration()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            Configuration = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .Build();
        }

        /// <summary>
        /// Inicializálja az API klienst a konfigurációban megadott alapcím használatával
        /// Validálja az API URL-t, mielőtt létrehozná a kliens példányt
        /// </summary>
        private void InitializeApi()
        {
            var baseUrl = Configuration["ApiBaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "Az ApiBaseUrl nincs beállítva az appsettings.json fájlban. Kérem ellenőrizze a konfigurációt.");
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    $"Érvénytelen ApiBaseUrl formátum: {baseUrl}. Az URL-nek abszolútnak kell lennie (pl. http://localhost:3001)");
            }

            Api = new Api(baseUrl);
        }

        /// <summary>
        /// Alkalmazás kilépésekor fut le
        /// Tisztítja az erőforrásokat (API kliens)
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            // Erőforrások tisztítása ha szükséges
            Api?.Dispose();
            base.OnExit(e);
        }
    }
}
