using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using FileReport47.Models;
using FileReport47.Services;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace FileReport47
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [STAThread]
        static async Task Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                // Mode ligne de commande : on s'assure que la console est visible
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_SHOW);
                await RunCommandLineMode(args[0]);
            }
            else
            {
                // Mode GUI : on cache la console
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
                Application.Run(new MainForm());
            }
        }

        private static async Task RunCommandLineMode(string settingsPath)
        {
            try
            {
                if (!File.Exists(settingsPath))
                {
                    Console.WriteLine($"Error: Settings file not found: {settingsPath}");
                    return;
                }

                var settings = JsonConvert.DeserializeObject<SearchParameters>(File.ReadAllText(settingsPath));
                
                if (!Directory.Exists(settings.SearchPath))
                {
                    Console.WriteLine($"Error: Search directory does not exist: {settings.SearchPath}");
                    return;
                }

                var searchService = new FileSearchService();
                var progress = new Progress<(int matched, int total)>(p => 
                {
                    Console.Write($"\rProcessing files: {p.matched} matches found out of {p.total} files processed");
                });

                Console.WriteLine($"Starting search in: {settings.SearchPath}");
                Console.WriteLine($"Output will be saved to: {settings.OutputPath}");
                Console.WriteLine($"Filters: {string.Join("; ", settings.FileFilters)}");
                Console.WriteLine();

                await searchService.SearchFilesAsync(settings, progress);
                Console.WriteLine("\nSearch completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }
    }
}
