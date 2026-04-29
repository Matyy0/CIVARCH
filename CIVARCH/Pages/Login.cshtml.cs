using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Claims;

namespace CIVARCH.Pages
{
    [AllowAnonymous]
    public class LoginModel(IConfiguration config) : PageModel
    {
        private readonly IConfiguration _config = config;

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(string.Empty, "Neplatné přihlašovací údaje");
                return Page();
            }

            if (AuthenticateAndCheckGroup(Username, Password, _config["LdapSettings:Group"]))
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, Username) };
                var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return RedirectToPage("/Index");
            }

            ModelState.AddModelError(string.Empty, "Neplatné přihlašovací údaje");
            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }

        bool AuthenticateAndCheckGroup(string userUpn, string password, string? groupName)
        {
            var ldapServer = _config["LdapSettings:Server"];
            var port       = Convert.ToInt32(_config["LdapSettings:Port"]);

            var connection = new LdapConnection(
                new LdapDirectoryIdentifier(ldapServer, port),
                new NetworkCredential(userUpn, password),
                AuthType.Basic);
            connection.SessionOptions.ProtocolVersion = 3;
            try
            {
                connection.Bind();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
