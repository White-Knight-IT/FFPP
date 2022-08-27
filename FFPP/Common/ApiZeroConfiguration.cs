using System.Text.Json;
using System.Text;
using FFPP.Api.v10.Tenants;
using FFPP.Data.Logging;
using System;

namespace FFPP.Common
{
    /// <summary>
	/// This class is used to handle anything we can automatically configure, the
    /// idea is to reduce the amount of configuration the user has to do mandatory
    /// to the absolute minimum necessary, this allows us to setup secure defaults etc
	/// </summary>
    public class ApiZeroConfiguration
    {
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? Domain { get; set; }
        public string? Instance { get; set; }
        public string? Scopes { get; set; }
        public string? AuthorizationUrl { get; set; }
        public string? TokenUrl { get; set; }
        public string? ApiScope { get; set; }
        public string? OpenIdClientId { get; set; }
        public string? CallbackPath { get; set; }
        public string? AppPassword { get; set; }
        public string? RefreshToken{ get; set; }
        public string? ExchangeRefreshToken { get; set; }
        public bool? IsBootstrapped { get; set; }

        public static async Task<bool> Setup(string ownerTenant="")
        {
            // TenantId is GUID (CustomerId) and not domain
            if (!ownerTenant.Contains('.'))
            {
                ownerTenant = await Tenant.GetDefaultDomainFromCustomerId(ownerTenant);
            }

            string domain = ownerTenant;
            string scopes = ApiEnvironment.ApiAccessScope;
            string authorizationUrl = string.Format("https://login.microsoftonline.com/{0}/oauth2/v2.0/authorize",ownerTenant);
            string tokenUrl = string.Format("https://login.microsoftonline.com/{0}/oauth2/v2.0/token", ownerTenant);
            string instance = "https://login.microsoftonline.com/";
            string apiScopeGuid = Guid.NewGuid().ToString();

            // step one - create SAM SPA that the Swagger UI will use to authenticate

            JsonElement samSpa = (await Sam.CreateSAMAuthApp($"FFPP UI - {ApiEnvironment.DeviceTag}", Sam.SamAppType.Spa, domain, spaRedirectUri: new string[] { string.Format("{0}/swagger/oauth2-redirect.html", ApiEnvironment.FfppFrontEndUri.TrimEnd('/')), string.Format("{0}/index.html", ApiEnvironment.FfppFrontEndUri.TrimEnd('/'))})).sam;
            string openIdClientId = samSpa.GetProperty("appId").GetString() ?? string.Empty;
            if (!openIdClientId.Equals(string.Empty))
            {
                // Wait 30 seconds to ensure the SPA gets registered
                await Task.Delay(30000);

                // step two - create SAM that will act as the authentication hub of the API
                Sam.SamAndPassword result = await Sam.CreateSAMAuthApp($"FFPP API - {ApiEnvironment.DeviceTag}", Sam.SamAppType.Api, domain, openIdClientId, scopeGuid: apiScopeGuid);
                JsonElement samApi = result.sam;
                string? appPassword = result.appPassword;
                string clientId = samApi.GetProperty("appId").GetString() ?? string.Empty;
                string idUri = samApi.GetProperty("identifierUris").EnumerateArray().ToArray()[0].GetString() ?? string.Empty;
                string apiScope = string.Format("{0}/{1}", idUri, ApiEnvironment.ApiAccessScope);

                if (!clientId.Equals(string.Empty))
                {
                    ApiZeroConfiguration zeroConf = new()
                    {
                        TenantId = ownerTenant,
                        ClientId = clientId,
                        Domain = domain,
                        Instance = instance,
                        Scopes = scopes,
                        AuthorizationUrl = authorizationUrl,
                        TokenUrl = tokenUrl,
                        ApiScope = apiScope,
                        OpenIdClientId = openIdClientId,
                        CallbackPath = "/signin-oidc",
                        AppPassword = appPassword
                 
                    };

                    zeroConf.Save();

                    // Setup our front end config file
                    await File.WriteAllTextAsync($"{ApiEnvironment.WebRootPath}/config.js",$@"/* Don't put secret configuration settings in this file, this is rendered
by the client. */

const config = {{
  auth: {{
    clientId: '{zeroConf.OpenIdClientId}',
    authority: 'https://login.microsoftonline.com/organizations/',
    redirectUri: '/index.html',
    postLogoutRedirectUri: '/bye.html'
  }},
  cache: {{
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false
  }},
  api: {{
    scopes: ['{zeroConf.ApiScope}'],
    requiresInit: true
  }}
}};");
                }
            }

            return false;
        }

        public static bool ImportApiZeroConf(ref WebApplicationBuilder builder)
        {
            try
            {
                ApiZeroConfiguration? zero = ApiZeroConfiguration.Read().Result;

                if (zero != null)
                {
                    ApiEnvironment.Secrets.TenantId = zero.TenantId;
                    ApiEnvironment.Secrets.ApplicationId = zero.ClientId;
                    ApiEnvironment.Secrets.ApplicationSecret = zero.AppPassword;
                    ApiEnvironment.Secrets.RefreshToken = zero.RefreshToken;
                    ApiEnvironment.Secrets.ExchangeRefreshToken = zero.ExchangeRefreshToken;
                    ApiEnvironment.IsBoostrapped = zero.IsBootstrapped ?? false;
                    builder.Configuration["ZeroConf:AzureAd:TenantId"] = zero.TenantId;
                    builder.Configuration["ZeroConf:AzureAd:ClientId"] = zero.ClientId;
                    builder.Configuration["ZeroConf:AzureAd:Domain"] = zero.Domain;
                    builder.Configuration["ZeroConf:AzureAd:Scopes"] = zero.Scopes;
                    builder.Configuration["ZeroConf:AzureAd:AuthorizationUrl"] = zero.AuthorizationUrl;
                    builder.Configuration["ZeroConf:AzureAd:TokenUrl"] = zero.TokenUrl;
                    builder.Configuration["ZeroConf:AzureAd:ApiScope"] = zero.ApiScope;
                    builder.Configuration["ZeroConf:AzureAd:OpenIdClientId"] = zero.OpenIdClientId;
                    builder.Configuration["ZeroConf:AzureAd:Instance"] = zero.Instance;
                    builder.Configuration["ZeroConf:AzureAd:CallbackPath"] = zero.CallbackPath;
                    return true;
                }
                else if (!ApiEnvironment.CheckForBootstrap().Result)
                {
                    Console.WriteLine($"Waiting for bootstrap.json to be placed at {ApiEnvironment.PersistentDir} to provision the API...");
                }
            }
            catch(Exception ex)
            {
                ApiEnvironment.RunErrorCount++;
                Console.WriteLine($"Exception reading ApiZeroConfiguration file: {ex.Message}");
            }

            return false;
        }

        public static async Task<ApiZeroConfiguration?> Read()
        {
            string apiZeroConfPath = $"{ApiEnvironment.PersistentDir}/api.zeroconf.json";

            if (File.Exists(apiZeroConfPath))
            {
                return await Utilities.ReadJsonFromFile<ApiZeroConfiguration>(apiZeroConfPath, true);
            }

            return null;
        }

        public bool Save()
        {
            try
            {
                Utilities.WriteJsonToFile<ApiZeroConfiguration>(this, $"{ApiEnvironment.PersistentDir}/api.zeroconf.json", true);
                ApiEnvironment.Secrets.TenantId = this.TenantId;
                ApiEnvironment.Secrets.ApplicationId = this.ClientId;
                ApiEnvironment.Secrets.ApplicationSecret = this.AppPassword;
                ApiEnvironment.Secrets.RefreshToken = this.RefreshToken;
                ApiEnvironment.Secrets.ExchangeRefreshToken = this.ExchangeRefreshToken;
                ApiEnvironment.IsBoostrapped = this.IsBootstrapped ?? false;
                return true;
            }
            catch(Exception ex)
            {
                ApiEnvironment.RunErrorCount++;
                Console.WriteLine($"Exception saving ApiZeroConfiguration file: {ex.Message}");
            }

            return false;
        }
    }
}

