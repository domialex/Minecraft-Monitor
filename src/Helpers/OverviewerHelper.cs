using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Minecraft_Monitor.Helpers
{
    public static class OverviewerHelper
    {
        private static List<string> DefaultOverviewerFiles { get; set; } = new()
        {
            "index.html",
            "leaflet.js",
            "overviewer.js",
            "overviewerConfig.js"
        };

        /// <summary>
        /// Checks if the expected files to be found in the Overviewer Output path are accessible.
        /// </summary>
        public static bool IsOverviewerOutputPathValid(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   DefaultOverviewerFiles.All(x => File.Exists(Path.Combine(path, x)));
        }
    }
}