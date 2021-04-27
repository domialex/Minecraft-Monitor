using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minecraft_Monitor.Models;

namespace Minecraft_Monitor.Middlewares
{
    /// <summary>
    /// Custom StaticFileMiddleware that allows the web server to serve static files from the Overviewer output folder.
    /// The middleware is last in the pipeline and allows the default blazor logic to handle requests first.
    /// </summary>
    public class OverviewerStaticFileMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IWebHostEnvironment env;
        private readonly ILoggerFactory loggerFactory;
        private StaticFileMiddleware staticFileMiddleware { get; set; }
        private string currentOverviewerOutputPath { get; set; }
        public OverviewerStaticFileMiddleware(RequestDelegate next,
                                              IWebHostEnvironment env,
                                              ILoggerFactory loggerFactory)
        {
            this.next = next;
            this.env = env;
            this.loggerFactory = loggerFactory;
        }

        public async Task Invoke(HttpContext context, MinecraftMonitorContext minecraftMonitorContext)
        {
            await next(context);

            var overviewerOutputPath = await GetOverviewerOutputPath(minecraftMonitorContext);
            if (overviewerOutputPath != null)
            {
                // Only create a new StaticFileMiddleware when the path has changed.
                if (currentOverviewerOutputPath != overviewerOutputPath)
                {
                    currentOverviewerOutputPath = overviewerOutputPath;
                    staticFileMiddleware = CreateStaticFileMiddleware();
                }

                await staticFileMiddleware.Invoke(context);
            }
        }

        private StaticFileMiddleware CreateStaticFileMiddleware()
        {
            var extensionProvider = new FileExtensionContentTypeProvider();
            extensionProvider.Mappings.Clear();
            extensionProvider.Mappings.Add(".css", "text/css");
            extensionProvider.Mappings.Add(".js", "application/javascript");
            extensionProvider.Mappings.Add(".png", "image/png");
            var options = new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(currentOverviewerOutputPath),
                ContentTypeProvider = extensionProvider
            };

            return new StaticFileMiddleware(next, env, Options.Create(options), loggerFactory);
        }

        private async Task<string> GetOverviewerOutputPath(MinecraftMonitorContext minecraftMonitorContext)
        {
            var settings = await minecraftMonitorContext.Settings.SingleAsync();

            return settings.OverviewerOutputPath;
        }
    }
}