
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Minecraft_Monitor.Controllers
{
    /// <summary>
    /// https://github.com/dotnet/AspNetCore.Docs/issues/15919#issuecomment-581457534
    /// </summary>
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        public static string SignInRoute = "/account/signin?t=";
        public static string Purpose = "SignIn";
        private readonly IDataProtector dataProtectionProvider;
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;

        public AccountController(IDataProtectionProvider dataProtectionProvider,
                                 UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager)
        {
            this.dataProtectionProvider = dataProtectionProvider.CreateProtector(Purpose);
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> SignIn(string t)
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                return BadRequest();
            }

            try
            {
                var data = dataProtectionProvider.Unprotect(t);
                var parts = data.Split('|');
                var identityUser = await userManager.FindByIdAsync(parts[0]);
                var isTokenValid = await userManager.VerifyUserTokenAsync(identityUser, TokenOptions.DefaultProvider, Purpose, parts[1]);

                if (isTokenValid)
                {
                    await signInManager.SignInAsync(identityUser, true);

                    return Redirect("/dashboard");
                }
            }
            catch { }

            return Unauthorized();
        }

        [Authorize]
        [HttpGet]
        public new async Task<IActionResult> SignOut()
        {
            await signInManager.SignOutAsync();

            return Redirect("/");
        }
    }
}