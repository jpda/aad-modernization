using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;

[assembly: OwinStartup(typeof(AadModernization.Startup))]

namespace AadModernization
{
    public class Startup
    {
        private readonly string _clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private readonly string _redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private readonly string _tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private readonly string _authority = $"{ConfigurationManager.AppSettings["ida:Instance"]}/{ConfigurationManager.AppSettings["ida:Tenant"]}/v2.0";
        private readonly string _clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];
        private readonly string[] _accessTokenScopes = new[] { "Directory.Read.All" };
        private readonly HttpClient _client = new HttpClient();

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
                    ResponseType = OpenIdConnectResponseType.CodeIdToken,
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        NameClaimType = "preferred_username"
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        SecurityTokenValidated = ctx =>
                        {
                            // here we could query a database, e.g., your existing authorization database. we can also add multiple here
                            //ctx.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Role, "AdministrativeUser"));
                            //ctx.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Role, "SystemUser"));
                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = ctx =>
                        {
                            ctx.HandleResponse();
                            ctx.Response.Redirect("/home/error?e=" + ctx.Exception.Message);
                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = async ctx =>
                        {
                            var groupList = ctx.AuthenticationTicket.Identity.Claims.Where(x => x.Type == "groups").ToList(); //prevent further enumerations
                            if (!groupList.Any()) return; // no groups, so nothing to do here

                            var appContext = ConfidentialClientApplicationBuilder.Create(_clientId).WithRedirectUri(_redirectUri).WithClientSecret(_clientSecret).WithTenantId(_tenant).Build();
                            var tokenRequest = appContext.AcquireTokenByAuthorizationCode(_accessTokenScopes, ctx.ProtocolMessage.Code);
                            var token = await tokenRequest.ExecuteAsync(); // catch msal errors here
                            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                            foreach (var gid in groupList.Select(x => x.Value))
                            {
                                var uri = $"https://graph.microsoft.com/v1.0/groups/{gid}";
                                var result = await _client.GetAsync(uri);
                                if (!result.IsSuccessStatusCode)
                                {
                                    if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                                    {
                                        // token isn't valid or isn't attached correctly, retry getting a token, perhaps
                                    }
                                    if (result.StatusCode == System.Net.HttpStatusCode.NotFound) { continue; } // move on
                                    throw new System.Exception($"Something wrong with the graph call: {result.StatusCode}");
                                }

                                var data = JObject.Parse(await result.Content.ReadAsStringAsync());
                                if (data.TryGetValue("displayName", out var groupName))
                                {
                                    ctx.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Role, groupName.Value<string>()));
                                }

                            }
                        }
                    }
                }
            );
        }
    }
}