using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minecraft_Monitor.Models;

namespace Minecraft_Monitor.Services
{
    public class OverviewerService
    {
        private readonly IDbContextFactory<MinecraftMonitorContext> minecraftMonitorContextFactory;
        private readonly ILogger<OverviewerService> logger;
        private readonly string currentDirectory;
        public event Action<int> OnDownloadProgressChanged;
        public bool IsDownloadingAndExtracting { get; private set; }

        public OverviewerService(IDbContextFactory<MinecraftMonitorContext> minecraftMonitorContextFactory,
                                 ILogger<OverviewerService> logger)
        {
            this.minecraftMonitorContextFactory = minecraftMonitorContextFactory;
            this.logger = logger;
            this.currentDirectory = Path.GetDirectoryName(Directory.GetCurrentDirectory());
        }

        /// <summary>
        /// Downloads Overviewer and extracts it in the folder ./overviewer next to Minecraft Monitor.
        /// </summary>
        public void DownloadAndExtract()
        {
            if (IsDownloadingAndExtracting)
            {
                return;
            }

            IsDownloadingAndExtracting = true;

            Task.Run(() =>
            {
                using (var client = new WebClient())
                {
                    using (var minecraftMonitorContext = minecraftMonitorContextFactory.CreateDbContext())
                    {
                        var settings = minecraftMonitorContext.Settings.Single();

                        var overviewerPath = Path.Combine(currentDirectory, "overviewer"); // ./overviewer/
                        var overviewerZipDestination = Path.Combine(currentDirectory, "overviewer.zip"); // ./overviewer/overviewer.zip

                        // Get the builds urls from overviewer.org.
                        var response = client.DownloadString("https://overviewer.org/downloads.json");
                        var json = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(response);

                        // Download and extract the win64 version.
                        client.DownloadFile(json["win64"]["url"], overviewerZipDestination);
                        client.DownloadProgressChanged += (s, e) => OnDownloadProgressChanged?.Invoke(e.ProgressPercentage);
                        ZipFile.ExtractToDirectory(overviewerZipDestination, overviewerPath, true);  // ./overviewer/overviewer-x.x.x/

                        settings.OverviewerExecutablePath = Path.Combine(overviewerPath, "overviewer-" + json["win64"]["version"], "overviewer.exe");
                        minecraftMonitorContext.SaveChanges();

                        // Delete the downloaded zip file.
                        //File.Delete(overviewerZipDestination);
                    }
                }

                IsDownloadingAndExtracting = false;
            });
        }
    }
}