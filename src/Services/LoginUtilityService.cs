using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Minecraft_Monitor.Controllers;

namespace Minecraft_Monitor.Services
{
    public class LoginUtilityService
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly NavigationManager navigationManager;
        private readonly IDataProtectionProvider dataProtectionProvider;
        public LoginUtilityService(UserManager<IdentityUser> userManager,
                                   NavigationManager navigationManager,
                                   IDataProtectionProvider dataProtectionProvider)
        {
            this.userManager = userManager;
            this.navigationManager = navigationManager;
            this.dataProtectionProvider = dataProtectionProvider;
        }

        /// <summary>
        /// Verifies that the user exists.
        /// Verifies that the password is correct.
        /// Generates a token and creates a URL in order to log in.
        /// </summary>
        public async Task<string> TryLoginAndGetLoginUrlAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var user = await userManager.FindByNameAsync(username);
            if (user == null || !await userManager.CheckPasswordAsync(user, password))
            {
                return null;
            }

            var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, AccountController.Purpose);

            var parsedQuery = HttpUtility.ParseQueryString(new Uri(navigationManager.Uri).Query);

            var protector = dataProtectionProvider.CreateProtector(AccountController.Purpose);

            var protectedData = protector.Protect($"{user.Id}|{token}");

            return AccountController.SignInRoute + protectedData;
        }
    }
}