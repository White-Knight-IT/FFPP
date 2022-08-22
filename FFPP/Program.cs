/// Freakin' Fast Partner Portal
/// Created by Ian Harris (@knightian) - White Knight IT - https://whiteknightit.com.au
/// 2022-07-05
/// Licensed under the MIT License

using FFPP.Common;
using FFPP.Api.v10.Tenants;
using FFPP.Api.v10.Licenses;
using FFPP.Api.v10.Users;
using FFPP.Api.v10.Dashboards;
using ApiCurrent = FFPP.Api;
using ApiV10 = FFPP.Api.v10;
using ApiDev = FFPP.Api.v11;
using ApiBootstrap = FFPP.Api.Bootstrap;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using FFPP.Api.Bootstrap;
using System.Text;

Console.WriteLine(@"
    ________________  ____ 
   / ____/ ____/ __ \/ __ \
  / /_  / /_  / /_/ / /_/ /
 / __/ / __/ / ____/ ____/ 
/_/   /_/   /_/   /_/      
                           
Freakin' Fast Partner Portal

Created by Ian Harris (@knightian) - White Knight IT - https://whiteknightit.com.au

2022-07-05

Licensed under the AGPL-3.0 License + Security License Addendum

v" + ApiEnvironment.ApiBinaryVersion+@"
");

var builder = WebApplication.CreateBuilder(args);

// Load individual settings
ApiEnvironment.UseHttpsRedirect = builder.Configuration.GetValue<bool>("ApiSettings:HttpsRedirect");
ApiEnvironment.ShowDevEnvEndpoints = builder.Configuration.GetValue<bool>("ApiSettings:ShowDevEndpoints");
ApiEnvironment.ShowSwaggerUi = builder.Configuration.GetValue<bool>("ApiSettings:ShowSwaggerUi");
ApiEnvironment.RunSwagger = builder.Configuration.GetValue<bool>("ApiSettings:RunSwagger");
ApiEnvironment.ServeStaticFiles = builder.Configuration.GetValue<bool>("ApiSettings:ServeStaticFiles");
ApiEnvironment.MysqlUser = builder.Configuration.GetValue<string>("ApiSettings:DbSettings:MysqlUser") ?? "ffppapiservice";
ApiEnvironment.MysqlPassword = builder.Configuration.GetValue<string>("ApiSettings:DbSettings:MysqlPassword") ?? "wellknownpassword";
ApiEnvironment.MysqlServer = builder.Configuration.GetValue<string>("ApiSettings:DbSettings:MysqlServer") ?? "localhost";
ApiEnvironment.MysqlServerPort = builder.Configuration.GetValue<string>("ApiSettings:DbSettings:MysqlServerPort") ?? "7704";
ApiEnvironment.CacheDir = builder.Configuration.GetValue<string>("ApiSettings:CachePath") ?? $"{ApiEnvironment.DataDir}/Cache";
ApiEnvironment.PersistentDir = builder.Configuration.GetValue<string>("ApiSettings:PersistentPath") ?? ApiEnvironment.WorkingDir;
ApiEnvironment.WebRootPath = builder.Configuration.GetValue<string>("ApiSettings:WebRootPath") ?? $"{ApiEnvironment.WorkingDir}/wwwroot";
ApiEnvironment.FfppFrontEndUri = builder.Configuration.GetValue<string>("ApiSettings:WebUiUrl") ?? "http://localhost";
ApiEnvironment.DeviceTag = await ApiEnvironment.GetDeviceTag();
string kestrelHttps = builder.Configuration.GetValue<string>("Kestrel:Endpoints:Https:Url") ?? "https://localhost:7074";
string kestrelHttp = builder.Configuration.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "https://localhost:7073";

// These bytes form the basis of persistent but importantly unique seed entropy throughout crypto functions in this API
await ApiEnvironment.GetEntropyBytes();

// We will import our ApiZeroConf settings else try find bootstrap app to build from
while (!ApiZeroConfiguration.ImportApiZeroConf(ref builder))
{
    Console.WriteLine("Waiting for bootstrap.json to provision the API...");
    Thread.CurrentThread.Join(10000);
}

// Ties the API to an Azure AD app for auth
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "ZeroConf:AzureAd");

// CORS policy to allow the UI to access the API
string[] corsUris = new string[]{ ApiEnvironment.FfppFrontEndUri, kestrelHttps, kestrelHttp } ?? new string[]{kestrelHttps,kestrelHttp};

builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
{
    // This allows for our Web UI which may be at a totally different domain and/or port to comminucate with the API
    builder.WithOrigins(corsUris).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));

// Build Data/Cache directories if they don't exist
ApiEnvironment.DataAndCacheDirectoriesBuild();

// Add auth services
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Add API versioning capabilities
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.DefaultApiVersion = ApiEnvironment.ApiCurrent;
}).AddApiExplorer(options =>
{
    options.SubstitutionFormat = "VV";
    options.GroupNameFormat = "'v'VV";
    options.SubstituteApiVersionInUrl = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
});

// Configure JSON options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.AllowTrailingCommas = false;

    // Official CIPP-API has absolutely no standards for serializing JSON, we need this to match it, and it hurts my soul immensly.
    options.SerializerOptions.PropertyNamingPolicy = null;
});

builder.WebHost.UseUrls();

await ApiEnvironment.UpdateDbContexts();

// Expose development environment API endpoints if set in settings to do so
if (ApiEnvironment.ShowDevEnvEndpoints)
{
    ApiEnvironment.ApiRouteVersions.Add(double.Parse(ApiEnvironment.ApiDev.ToString()));
}

if (ApiEnvironment.IsDebug)
{
    Console.WriteLine("######################## FFPP is running in DEBUG context");

    // In dev env we can get secrets from local environment (use `dotnet user-secrets` tool to safely store local secrets)
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("######################## FFPP is running from a development environment");
    }
}

// Prep Swagger and specify the auth settings for it to use a SAM on Azure AD
builder.Services.AddSwaggerGen(customSwagger => {

    foreach (double version in ApiEnvironment.ApiRouteVersions)
    {
        if (version.ToString("f1").Contains(ApiEnvironment.ApiDev.ToString()))
        {
            customSwagger.SwaggerDoc(string.Format("v{0}", version.ToString("f1")), new() { Title = "FFPP API DEV", Version = string.Format("v{0}", version.ToString("f1")) });
            continue;
        }
        customSwagger.SwaggerDoc(string.Format("v{0}", version.ToString("f1")), new() { Title = "FFPP API", Version = string.Format("v{0}", version.ToString("f1")) });

    }

    customSwagger.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "OAuth2.0 Auth Code with PKCE",
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(builder.Configuration["ZeroConf:AzureAd:AuthorizationUrl"]),
                TokenUrl = new Uri(builder.Configuration["ZeroConf:AzureAd:TokenUrl"]),
                Scopes = new Dictionary<string, string>
                {
                    { builder.Configuration["ZeroConf:AzureAd:ApiScope"], builder.Configuration["ZeroConf:AzureAd:Scopes"]}
                }
            }
        }
    });
    customSwagger.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new[] { builder.Configuration["ZeroConf:AzureAd:ApiScope"] }
        }
    });
});

var app = builder.Build();

app.UseCors("corsapp");

app.UseAuthentication();
app.UseAuthorization();

if (ApiEnvironment.UseHttpsRedirect)
{
    // Redirect HTTP to HTTPS, seems to use 307 temporary redirect
    app.UseHttpsRedirection();
}

if (ApiEnvironment.ServeStaticFiles)
{
    // Allows us to serve files from wwwroot to customise swagger etc.
    app.UseStaticFiles();
}

ApiVersionSetBuilder apiVersionSetBuilder = app.NewApiVersionSet();

foreach (double version in ApiEnvironment.ApiRouteVersions)
{
    apiVersionSetBuilder.HasApiVersion(new(version));
}

ApiEnvironment.ApiVersionSet = apiVersionSetBuilder.ReportApiVersions().Build();

// /bootstrap special API endpoints for bootstrapping this API, not used by anything other than FFPP realistically
ApiBootstrap.BootstrapRoutes.InitRoutes(ref app);

// /x.x (ApiEnvironment.ApiDev) path which uses the latest devenv API specification (will only be accessible if ShowDevEnvEndpoints = true)
ApiDev.Routes.InitRoutes(ref app);

// /api path which always uses the latest API specification
ApiCurrent.Routes.InitRoutes(ref app);

// /v1.0 path using API specification v1.0
ApiV10.Routes.InitRoutes(ref app);

if (ApiEnvironment.RunSwagger)
{
    app.UseSwagger();

    // But we don't always show the UI for swagger
    if (ApiEnvironment.ShowSwaggerUi)
    {
        app.UseSwaggerUI(customSwagger =>
        {

            foreach (var desc in app.DescribeApiVersions())
            {
                var url = $"/swagger/{desc.GroupName}/swagger.json";
                var name = desc.GroupName.ToUpperInvariant();
                if (desc.ApiVersion.ToString().Contains(ApiEnvironment.ApiDev.ToString()))
                {
                    customSwagger.SwaggerEndpoint(url, $"FFPP API DEV {name}");
                    continue;
                }
                customSwagger.SwaggerEndpoint(url, $"FFPP API {name}");
            }

            customSwagger.InjectStylesheet("/swagger-customisation/swagger-customisation.css");
            customSwagger.OAuthClientId(app.Configuration["ZeroConf:AzureAd:OpenIdClientId"]);
            customSwagger.OAuthUsePkce();
            customSwagger.OAuthScopeSeparator(" ");
        });
    }
}

//string pname = License.ConvertSkuName("SPE_E3_RPA1", string.Empty);
//await new FfppLogs().LogDb.LogRequest("Test Message", "", "Information", "M365B654613.onmicrosoft.com", "ThisIsATest");
//List<Tenant> tenants = await Tenant.GetTenants(string.Empty);
//await RequestHelper.NewTeamsApiGetRequest("https://api.interfaces.records.teams.microsoft.com/Skype.TelephoneNumberMgmt/Tenants/b439f90e-eb4a-40f3-b11a-d793c488b38a/telephone-numbers?locale=en-US", "b439f90e-eb4a-40f3-b11a-d793c488b38a", HttpMethod.Get);
//await RequestHelper.GetClassicApiToken("M365B654613.onmicrosoft.com", "https://outlook.office365.com");
//var code = await RequestHelper.NewDeviceLogin("a0c73c16-a7e3-4564-9a95-2bdf47383716", "https://outlook.office365.com/.default", true, "", "M365B654613.onmicrosoft.com");
//await MsolUser.GetCippMsolUsers("M365B654613.onmicrosoft.com");
//var ebay = await FfppDashboard.GetHomeData();
//FfppDashboard.CheckVersions("2.9.0");
//var salt = Utilities.Random2WordPhrase(24);
//List<Domain> domains = await Domain.GetDomains("", ApiEnvironment.Secrets.TenantId);

app.Run();