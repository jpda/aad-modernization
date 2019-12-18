using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

[assembly: OwinStartup(typeof(AadModernization.Startup))]

namespace AadModernization
{
    public class Startup
    {
        private readonly string _clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private readonly string _redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private readonly string _authority = $"{ConfigurationManager.AppSettings["ida:Instance"]}/{ConfigurationManager.AppSettings["ida:Tenant"]}/v2.0";

        public void Configuration(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = _clientId,
                    Authority = _authority,
                    RedirectUri = _redirectUri,
                    PostLogoutRedirectUri = _redirectUri,
                    Scope = OpenIdConnectScope.OpenIdProfile,
                    ResponseType = OpenIdConnectResponseType.IdToken,
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        NameClaimType = "preferred_username"
                    }
                }
            );
        }
    }
}