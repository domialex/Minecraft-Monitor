using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minecraft_Monitor.Models;
using RconSharp;

namespace Minecraft_Monitor.Services
{
    /// <summary>
    /// Wrapper around <see cref="RconSharp" />.
    /// This connection is kept alive.
    /// </summary>
    public class RconService
    {
        private readonly IDbContextFactory<MinecraftMonitorContext> minecraftMonitorContextFactory;
        private readonly ILogger<RconService> logger;

        private readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(10);
        private RconMessenger client { get; set; }
        public bool Connected { get; private set; }

        public RconService(IDbContextFactory<MinecraftMonitorContext> minecraftMonitorContextFactory,
                           ILogger<RconService> logger)
        {
            this.minecraftMonitorContextFactory = minecraftMonitorContextFactory;
            this.logger = logger;
        }

        /// <summary>
        /// Every RCON call here will throw an exception is the server is not responding.
        /// </summary>
        private async Task Connect()
        {
            using (var minecraftMonitorContext = minecraftMonitorContextFactory.CreateDbContext())
            {
                client?.Dispose();
                client = new RconMessenger();

                var settings = await minecraftMonitorContext.Settings.SingleAsync();

                logger.LogInformation($"Connecting to {settings.MinecraftHostname}:{settings.MinecraftPort}...");

                var isConnected = await client.ConnectAsync(settings.MinecraftHostname, settings.MinecraftPort);
                var isAuthenticated = await client.AuthenticateAsync(settings.MinecraftPassword);

                Connected = isConnected && isAuthenticated;

                if (Connected)
                {
                    logger.LogInformation("Connection successful.");
                }
            }
        }

        /// <summary>
        /// As soon as the command times out or there is an error, we flush the client.
        /// </summary>
        public async Task<string> ExecuteCommandAsync(string command)
        {
            string result = null;

            try
            {
                if (!Connected)
                {
                    await Connect();
                }

                var task = client.ExecuteCommandAsync(command);
                if (await Task.WhenAny(task, Task.Delay(TIMEOUT)) == task)
                {
                    result = task.Result;
                }
                else
                {
                    client.CloseConnection();
                    throw new Exception($"Server did not respond after {TIMEOUT.TotalSeconds} seconds.");
                }
            }
            catch
            {
                logger.LogError("Could not connect to the server. Verify that the RCON connection is correct.");
                Connected = false;
                client = null;
            }

            return result;
        }
    }
}