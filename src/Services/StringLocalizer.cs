using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace Minecraft_Monitor
{
    /// <summary>
    /// Simple implementation of a <see cref="IStringLocalizer" />.
    /// </summary>
    public class StringLocalizer : IStringLocalizer
    {
        private string twoLetterISOLanguageName;

        public static readonly string[] SupportedCultures = new[] { "en", "fr" };

        public StringLocalizer()
        {
            this.twoLetterISOLanguageName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        }

        public LocalizedString this[string name]
        {
            get
            {
                var success = TryGetResource(name, out string value);
                return new LocalizedString(name, value, resourceNotFound: !success);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var success = TryGetResource(name, out string value);
                return new LocalizedString(name, string.Format(value, arguments), resourceNotFound: !success);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Translations[twoLetterISOLanguageName].Keys.Select(x => new LocalizedString(x, Translations[twoLetterISOLanguageName][x]));
        }

        private bool TryGetResource(string key, out string value)
        {
            return Translations[twoLetterISOLanguageName].TryGetValue(key, out value) || Translations["en"].TryGetValue(key, out value);
        }

        public const string Dashboard = "Dashboard";
        public const string WelcomeTitle = "WelcomeTitle";
        public const string WelcomeBody = "WelcomeBody";
        public const string Map = "Map";
        public const string About = "About";
        public const string Hostname = "Hostname";
        public const string Port = "Port";
        public const string Username = "Username";
        public const string Password = "Password";
        public const string Required = "Required";
        public const string EnableMonitor = "EnableMonitor";
        public const string DisableMonitor = "DisableMonitor";
        public const string MonitorStarted = "MonitorStarted";
        public const string MonitorStopped = "MonitorStopped";
        public const string MonitorStatusLabel = "MonitorStatusLabel";
        public const string Save = "Save";
        public const string Saved = "Saved";
        public const string SettingsWidgetTitle = "SettingsWidgetTitle";
        public const string ConnectionStatus = "ConnectionStatus";
        public const string Connected = "Connected";
        public const string NotConnected = "NotConnected";
        public const string LastUpdate = "LastUpdate";
        public const string OverviewerOutputPath = "OverviewerOutputPath";
        public const string LogIn = "LogIn";
        public const string LogOut = "LogOut";
        public const string MapNotConfigured = "MapNotConfigured";
        public const string PortHelperText = "PortHelperText";
        public const string OverviewerOutputPathHelperText = "OverviewerOutputPathHelperText";
        public const string OverviewerOutputPathErrorText = "OverviewerOutputPathErrorText";
        public const string DisableMap = "DisableMap";
        public const string AboutText = "AboutText";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            {
                "en",
                new()
                {
                    [Dashboard] = "Dashboard",
                    [WelcomeTitle] = "Welcome to Minecraft Monitor",
                    [WelcomeBody] = $"To begin, you must configure Minecraft Monitor to be able to connect to your Minecraft Server using RCON.",
                    [Map] = "Map",
                    [About] = "About",
                    [Hostname] = "Hostname",
                    [Port] = "Port",
                    [Username] = "Username",
                    [Password] = "Password",
                    [Required] = "Required",
                    [EnableMonitor] = "Enable server monitoring",
                    [DisableMonitor] = "Disable server monitoring",
                    [MonitorStarted] = "Server monitoring started",
                    [MonitorStopped] = "Server monitoring stopped",
                    [MonitorStatusLabel] = "Monitor status:",
                    [Save] = "Save changes",
                    [Saved] = "Saved",
                    [SettingsWidgetTitle] = "RCON and Overviewer settings",
                    [ConnectionStatus] = "Connection status:",
                    [Connected] = "Connected",
                    [NotConnected] = "Not connected",
                    [LastUpdate] = "Last update:",
                    [OverviewerOutputPath] = "Overviewer output path",
                    [LogIn] = "Log in",
                    [LogOut] = "Log out",
                    [MapNotConfigured] = "The map is not configured.",
                    [PortHelperText] = "0 to 65535, default 25575",
                    [OverviewerOutputPathHelperText] = "This is the path that corresponds to 'outputdir' in your Overviewer config.",
                    [OverviewerOutputPathErrorText] = "The path is incorrect, unaccessible by permissions, or does not contain the expected Overviewer files.",
                    [DisableMap] = "Disable map",
                    [AboutText] = "Minecraft Monitor is possible thanks to the following technologies.",
                }
            },
            {
                "fr",
                new()
                {
                    [Dashboard] = "Tableau de bord",
                    [WelcomeTitle] = "Bienvenue sur Minecraft Monitor",
                    [WelcomeBody] = "Pour d??buter, vous devez configurer Minecraft Monitor pour qu'il puisse se connecter en RCON ?? votre serveur Minecraft.",
                    [Map] = "Carte",
                    [About] = "?? propos",
                    [Hostname] = "Nom d'h??te",
                    [Port] = "Port",
                    [Username] = "Utilisateur",
                    [Password] = "Mot de passe",
                    [Required] = "Champ requis",
                    [EnableMonitor] = "Activer le service de monitoring",
                    [DisableMonitor] = "D??sactiver le service de monitoring",
                    [MonitorStarted] = "Service de monitoring activ??",
                    [MonitorStopped] = "Service de monitoring arr??t??",
                    [MonitorStatusLabel] = "Statut du moniteur :",
                    [Save] = "Sauvegarder",
                    [Saved] = "Sauvegard??",
                    [SettingsWidgetTitle] = "Configurations RCON et Overviewer",
                    [ConnectionStatus] = "Statut de la connexion :",
                    [Connected] = "Connect??",
                    [NotConnected] = "Non connect??",
                    [LastUpdate] = "Derni??re mise ?? jour :",
                    [OverviewerOutputPath] = "R??pertoire de la carte de Overviewer",
                    [LogIn] = "Se connecter",
                    [LogOut] = "Se d??connecter",
                    [MapNotConfigured] = "La carte n'est pas configur??e.",
                    [PortHelperText] = "0 ?? 65535, par d??fault 25575",
                    [OverviewerOutputPathHelperText] = "Ceci est le r??pertoire qui correspond ?? 'outputdir' dans votre configuration d'Overviewer .",
                    [OverviewerOutputPathErrorText] = "Le r??pertoire est incorrect, inaccessible  ?? causes des permissions, ou ne contient pas les fichiers attendus d'Overviewer.",
                    [DisableMap] = "D??sactiver la carte",
                    [AboutText] = "Minecraft Monitor existe gr??ce aux technologies suivantes.",
                }
            }
        };

    }
}