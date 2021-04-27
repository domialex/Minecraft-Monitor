using System;

namespace Minecraft_Monitor.Models
{
    /// <summary>
    /// The application's settings.
    /// </summary>
    public class Settings
    {
        public int Id { get; set; }

        public bool IsOnboardingDone { get; set; }
        public string MinecraftHostname { get; set; } = "localhost";
        public ushort MinecraftPort { get; set; } = 25575;
        public string MinecraftPassword { get; set; } = "1234";

        public string OverviewerExecutablePath { get; set; }
        public string OverviewerOutputPath { get; set; }

        public bool IsMonitorServiceRunning { get; set; }
    }
}