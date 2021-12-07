using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Minecraft_Monitor.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Minecraft_Monitor.Helpers;

namespace Minecraft_Monitor.Services
{
    /// <summary>
    /// The main service that periodically gathers the information from the Minecraft server.
    /// </summary>
    public class MonitorService
    {
        private readonly IDbContextFactory<MinecraftMonitorContext> minecraftMonitorContextFactory;
        private readonly RconService rconService;
        private readonly CancellationToken cancellationToken;

        private TimeSpan interval;
        private DateTimeOffset lastHalfMinuteUpdateDateTime;
        public bool IsRunning;
        public event Action OnUpdate;
        public event Action OnStatusChange;

        private struct Commands
        {
            public const string LIST_UUIDS = "/list uuids";
            public const string GET_PLAYER_DIMENSIONS = "/execute as @a run data get entity @s Dimension";
            public const string GET_PLAYER_POSITIONS = "/execute as @a run data get entity @s Pos";
            public const string GET_PLAYER_INVENTORY = "/data get entity {0} Inventory";
            public const string GET_GAMETIME = "/time query gametime";
            public const string GET_DAYTIME = "/time query daytime";
        }

        private struct CommandResponses
        {
            public const string PLAYER_HAS_THE_FOLLOWING_DATA = "{0} has the following entity data: ";
            public const string NO_ENTITY_WAS_FOUND = "No entity was found";
            public const string NO_PLAYERS_ONLINE = "There are 0";
            public const string THE_TIME_IS = "The time is ";
        }

        public MonitorService(IDbContextFactory<MinecraftMonitorContext> minecraftMonitorContextFactory,
                              RconService rconService,
                              IHostApplicationLifetime applicationLifetime)
        {
            this.minecraftMonitorContextFactory = minecraftMonitorContextFactory;
            this.rconService = rconService;
            this.cancellationToken = applicationLifetime.ApplicationStopping;

            this.interval = TimeSpan.FromSeconds(5);

            using (var minecraftMonitorContext = minecraftMonitorContextFactory.CreateDbContext())
            {
                var serverInfo = minecraftMonitorContext.Settings.Single();
                if (serverInfo.IsMonitorServiceRunning)
                {
                    Start();
                }
            }
        }

        public async void Start()
        {
            using (var minecraftMonitorContext = minecraftMonitorContextFactory.CreateDbContext())
            {
                var serverInfo = await minecraftMonitorContext.Settings.SingleAsync();
                serverInfo.IsMonitorServiceRunning = true;
                await minecraftMonitorContext.SaveChangesAsync();
            }

            IsRunning = true;
            _ = Task.Run(() => WorkLoop());
            OnStatusChange?.Invoke();
        }

        public async void Stop()
        {
            using (var minecraftMonitorContext = minecraftMonitorContextFactory.CreateDbContext())
            {
                var serverInfo = await minecraftMonitorContext.Settings.SingleAsync();
                serverInfo.IsMonitorServiceRunning = false;
                await minecraftMonitorContext.SaveChangesAsync();
            }

            IsRunning = false;
            OnStatusChange?.Invoke();
        }

        /// <summary>
        /// Main loop that uses <see cref="RconService" />.
        /// </summary>
        private async Task WorkLoop()
        {
            while (!cancellationToken.IsCancellationRequested && IsRunning)
            {
                var halfAMinuteHasPassed = (DateTimeOffset.UtcNow - lastHalfMinuteUpdateDateTime).TotalSeconds >= 30;

                try
                {
                    using (var minecraftMonitorContext = minecraftMonitorContextFactory.CreateDbContext())
                    {
                        // Get server info.                    
                        var gameTime = await rconService.ExecuteCommandAsync(Commands.GET_GAMETIME);
                        if (gameTime == null)
                        {
                            throw new Exception();
                        }

                        var gameDayTime = await rconService.ExecuteCommandAsync(Commands.GET_DAYTIME);
                        var serverInfo = await minecraftMonitorContext.ServerInfo.SingleAsync();
                        serverInfo.GameTime = int.Parse(gameTime.Replace(CommandResponses.THE_TIME_IS, string.Empty));
                        serverInfo.DayTime = int.Parse(gameDayTime.Replace(CommandResponses.THE_TIME_IS, string.Empty));
                        serverInfo.IsRunning = true;
                        serverInfo.LastUpdate = DateTime.UtcNow;
                        await minecraftMonitorContext.SaveChangesAsync();

                        // Get the players' UUIDs.
                        var uuidsString = await rconService.ExecuteCommandAsync(Commands.LIST_UUIDS);
                        var dimensionsString = await rconService.ExecuteCommandAsync(Commands.GET_PLAYER_DIMENSIONS);
                        var positionsString = await rconService.ExecuteCommandAsync(Commands.GET_PLAYER_POSITIONS);

                        var players = await minecraftMonitorContext.Players.ToListAsync();

                        // Set all players as offline.
                        // Nothing else to fetch.
                        if (string.IsNullOrWhiteSpace(uuidsString) || uuidsString.Contains(CommandResponses.NO_PLAYERS_ONLINE))
                        {
                            minecraftMonitorContext.Players.ToList().ForEach(x => x.IsOnline = false);

                            await minecraftMonitorContext.SaveChangesAsync();
                        }
                        else
                        {
                            // Names and UUIDs.
                            var namesAndUUIDs = uuidsString.Substring(uuidsString.IndexOf(':') + 1)
                                                           .Split(',')
                                                           .Select(x => x.Trim()
                                                                         .Replace("(", string.Empty, StringComparison.Ordinal)
                                                                         .Replace(")", string.Empty, StringComparison.Ordinal)
                                                                         .Split(' '))
                                                           .Select(x => new { Name = x[0], UUID = Guid.Parse(x[1]) })
                                                           .ToList();

                            // Dimensions.
                            for (int i = 0; i < namesAndUUIDs.Count; i++)
                            {
                                dimensionsString = dimensionsString.Replace("\"", string.Empty)
                                                                   .Replace(string.Format(CommandResponses.PLAYER_HAS_THE_FOLLOWING_DATA, namesAndUUIDs[i].Name), i == 0 ? string.Empty : ",");
                            }
                            var dimensions = dimensionsString.Split(',');

                            // Coordinates.
                            var positions = positionsString.Replace(" ", string.Empty)
                                                           .Replace("d", string.Empty)
                                                           .Split('[', ']')
                                                           .Where((x, i) => i % 2 != 0)
                                                           .Select(x => x.Split(','))
                                                           .Select(x => new { X = (int)double.Parse(x[0]), Y = (int)double.Parse(x[1]), Z = (int)double.Parse(x[2]) })
                                                           .ToList();

                            for (int i = 0; i < namesAndUUIDs.Count(); i++)
                            {
                                var player = new Player();
                                var coordinates = new Coordinates()
                                {
                                    X = positions[i].X,
                                    Y = positions[i].Y,
                                    Z = positions[i].Z,
                                    Dimension = dimensions[i]
                                };

                                player.UUID = namesAndUUIDs[i].UUID;
                                player.Name = namesAndUUIDs[i].Name;
                                player.IsOnline = true;
                                player.LastOnlineDate = DateTimeOffset.UtcNow;

                                var entityPlayer = await minecraftMonitorContext.FindAsync<Player>(namesAndUUIDs[i].UUID);

                                // Only fetch player inventories after 30s.
                                if (halfAMinuteHasPassed)
                                {
                                    var nbtString = await rconService.ExecuteCommandAsync(string.Format(Commands.GET_PLAYER_INVENTORY, player.Name));
                                    if (nbtString != CommandResponses.NO_ENTITY_WAS_FOUND)
                                    {
                                        nbtString = nbtString.Replace(string.Format(CommandResponses.PLAYER_HAS_THE_FOLLOWING_DATA, player.Name), string.Empty);
                                        player.InventoryJson = NBTParser.GetJsonFromNBT(nbtString);
                                    }
                                }
                                else
                                {
                                    player.InventoryJson = entityPlayer.InventoryJson;
                                }

                                // Update.
                                if (entityPlayer != null)
                                {
                                    coordinates.Id = entityPlayer.Coordinates.Id;
                                    minecraftMonitorContext.Entry(entityPlayer.Coordinates).CurrentValues.SetValues(coordinates);
                                    minecraftMonitorContext.Entry(entityPlayer).CurrentValues.SetValues(player);
                                }
                                // Create.
                                else
                                {
                                    player.Coordinates = coordinates;
                                    minecraftMonitorContext.Players.Add(player);
                                }
                            }

                            // Set offline players.
                            minecraftMonitorContext.Players.Where(x => !namesAndUUIDs.Select(n => n.UUID).Contains(x.UUID))
                                                           .ToList()
                                                           .ForEach(x => x.IsOnline = false);

                            await minecraftMonitorContext.SaveChangesAsync();
                        }
                    }
                }
                catch { }
                {
                    using (var minecraftMonitorContext = minecraftMonitorContextFactory.CreateDbContext())
                    {
                        // We assume the server is down.
                        var serverInfo = await minecraftMonitorContext.ServerInfo.SingleAsync();
                        if (serverInfo != null)
                        {
                            serverInfo.IsRunning = false;
                            serverInfo.LastUpdate = DateTime.UtcNow;
                            await minecraftMonitorContext.SaveChangesAsync();
                        }
                    }
                }

                if (halfAMinuteHasPassed)
                {
                    lastHalfMinuteUpdateDateTime = DateTimeOffset.UtcNow;
                }

                OnUpdate?.Invoke();
                await Task.Delay(interval, cancellationToken);
            }
        }
    }
}