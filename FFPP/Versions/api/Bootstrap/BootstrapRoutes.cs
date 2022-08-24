using FFPP.Data;
using FFPP.Common;

namespace FFPP.Api.Bootstrap
{
    /// <summary>
    /// /api is basically a skeleton set of routes that always point to the latest /vX.Y api
    /// </summary>
    public static class BootstrapRoutes
    {
        public static void InitRoutes(ref WebApplication app)
        {
            /// <summary>
            /// /bootstrap/IsSetup
            /// </summary>
            app.MapGet("/bootstrap/IsSetup", async (HttpContext context, HttpRequest request) =>
            {
                Task<IsSetupResponse> checkCreds = new(() =>
                {
                    return new IsSetupResponse(){ isSetup = ApiEnvironment.HasCredentials };
                });

                checkCreds.Start();
                return await checkCreds;

            }).WithName("/bootstrap/IsSetup").ExcludeFromDescription();

            /// <summary>
            /// /bootstrap/GraphToken
            /// </summary>
            app.MapGet("/bootstrap/GetGraphToken", async (HttpContext context, HttpRequest request) =>
            {
                Task<bool> checkCreds = new(() =>
                {
                    return ApiEnvironment.HasCredentials;
                });

                checkCreds.Start();
                return await checkCreds;

            }).WithName("/bootstrap/GetGraphToken").ExcludeFromDescription();
        }

        public struct IsSetupResponse
        {
           public bool isSetup { get; set; }
        }
    }
}