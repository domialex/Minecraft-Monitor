using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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

            Task.Run(async () =>
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var minecraftMonitorContext = minecraftMonitorContextFactory.CreateDbContext())
                        {
                            var settings = minecraftMonitorContext.Settings.Single();

                            var overviewerPath = Path.Combine(currentDirectory, "overviewer"); // ./overviewer/
                            var overviewerZipDestination = Path.Combine(currentDirectory, "overviewer.zip"); // ./overviewer/overviewer.zip

                            // Get the builds urls from overviewer.org.
                            var json = await client.GetFromJsonAsync<Dictionary<string, Dictionary<string, string>>>("https://overviewer.org/downloads.json");

                            // Download and extract the win64 version.
                            var fileBytes = await client.GetByteArrayAsync(json["win64"]["url"]);
                            File.WriteAllBytes(overviewerZipDestination, fileBytes);
                            ZipFile.ExtractToDirectory(overviewerZipDestination, overviewerPath, true);  // ./overviewer/overviewer-x.x.x/

                            settings.OverviewerExecutablePath = Path.Combine(overviewerPath, "overviewer-" + json["win64"]["version"], "overviewer.exe");
                            minecraftMonitorContext.SaveChanges();

                            // Delete the downloaded zip file.
                            File.Delete(overviewerZipDestination);
                        }
                    }
                }
                catch
                {
                    logger.LogError("Could not download Overviewer.");
                }

                IsDownloadingAndExtracting = false;
            });
        }
    }
}