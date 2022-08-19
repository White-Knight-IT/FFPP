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
            /// /api/bootstrap/IsSetup
            /// </summary>
            app.MapGet("/api/bootstrap/IsSetup", async (HttpContext context, HttpRequest request) =>
            {
                Task<IsSetupResponse> checkCreds = new(() =>
                {
                    return new IsSetupResponse(){ isSetup = ApiEnvironment.HasCredentials };
                });

                checkCreds.Start();
                return await checkCreds;

            }).WithName("/api/bootstrap/IsSetup").ExcludeFromDescription();

            /// <summary>
            /// /api/bootstrap/IsSetup
            /// </summary>
            app.MapGet("/api/bootstrap/GetAADPSCode", async (HttpContext context, HttpRequest request) =>
            {
                Task<bool> checkCreds = new(() =>
                {
                    return ApiEnvironment.HasCredentials;
                });

                checkCreds.Start();
                return await checkCreds;

            }).WithName("/api/bootstrap/GetAADPSCode").ExcludeFromDescription();
        }

        public struct IsSetupResponse
        {
           public bool isSetup { get; set; }
        }
    }
}