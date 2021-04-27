using System;
using System.Text.Json.Serialization;

namespace Minecraft_Monitor.Models
{
    public class ServerInfo
    {
        [JsonIgnore]
        public int Id { get; set; }
        public bool IsRunning { get; set; }
        public int GameTime { get; set; }
        public int DayTime { get; set; }
        public DateTimeOffset LastUpdate { get; set; } = DateTime.Now;
    }
}