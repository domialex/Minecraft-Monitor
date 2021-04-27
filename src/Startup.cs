using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minecraft_Monitor.Models;
using Microsoft.OpenApi.Models;
using Minecraft_Monitor.Services;
using Serilog;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.Extensions.Localization;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Minecraft_Monitor.Middlewares;
using System;

namespace Minecraft_Monitor
{
    public class Startup
    {
        private IWebHostEnvironment appHost;
        public IConfiguration configuration;
        private const string ALLOWED_SPECIFIC_ORIGINS = "AllowAllOrigins";
        private string projectName;
        private string projectVersion;

        public Startup(IWebHostEnvironment appHost, IConfiguration configuration)
        {
            this.appHost = appHost;
            this.configuration = configuration;

            var assemblyName = GetType().Assembly.GetName();
            this.projectName = assemblyName.Name;
            this.projectVersion = assemblyName.Version.ToString();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Conditional(_ => configuration.GetValue<bool>("EnableLogFiles"),
                    sink => sink.Logger(logger => logger.WriteTo.File(
                        Path.Combine(Directory.GetCurrentDirectory(), ".log"),
                        fileSizeLimitBytes: 5 * 1024 * 1024,
                        retainedFileCountLimit: 5,
                        rollOnFileSizeLimit: true,
                        rollingInterval: RollingInterval.Day))
                )
                .CreateLogger();

            services.AddLogging(builder => builder.AddSerilog());
            services.AddSingleton((Serilog.ILogger)Log.Logger);

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddMvcCore().AddApiExplorer();

            services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
                config.SnackbarConfiguration.PreventDuplicates = false;
            });

            services.AddCors(options => options.AddPolicy(ALLOWED_SPECIFIC_ORIGINS, builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

            services.AddSwaggerGen(c => c.SwaggerDoc(projectVersion, new OpenApiInfo { Title = projectName, Version = projectVersion }));

            services.AddDbContextFactory<MinecraftMonitorContext>(options => options.UseSqlite("Data Source=database.db").UseLazyLoadingProxies());
            services.AddScoped<MinecraftMonitorContext>();

            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 0;
            }).AddEntityFrameworkStores<MinecraftMonitorContext>().AddDefaultTokenProviders();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", c => c.RequireRole("Admin"));
            });
            services.AddScoped<LoginUtilityService>();

            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

            services.AddSingleton<RconService>();
            services.AddSingleton<MonitorService>();
            services.AddSingleton<OverviewerService>();

            services.Configure<RequestLocalizationOptions>(options => options.AddSupportedUICultures(StringLocalizer.SupportedCultures));
            services.AddScoped<IStringLocalizer, StringLocalizer>();
        }

        private void UpgradeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<MinecraftMonitorContext>();
                if (context != null && context.Database != null)
                {
                    context.Database.Migrate();
                }
            }
        }

        public void Configure(IApplicationBuilder app,
                              IWebHostEnvironment env,
                              UserManager<IdentityUser> userManager,
                              RoleManager<IdentityRole> roleManager,
                              IConfiguration configuration)
        {
            MinecraftMonitorContext.EnsureAdminAndRolesExist(userManager, roleManager, configuration);

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{projectVersion}/swagger.json", projectName);
            });

            app.UseCors(ALLOWED_SPECIFIC_ORIGINS);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseRequestLocalization();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                var identity = context.User.Identity;
                await next.Invoke();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            app.UseMiddleware<OverviewerStaticFileMiddleware>();
        }


    }
}
