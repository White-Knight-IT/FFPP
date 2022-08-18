using FFPP.Common;

namespace FFPP.Api.v11
{
    /// <summary>
    /// /v1.1 ### THIS IS THE V1.1 ENDPOINTS (CURRENT DEV) ###
    /// </summary>
    public static class Routes
    {
        private static readonly string[] _tags = new[]{ "FFPP API DEV"};
        private static string _versionHeader = "v1.1";

        public static void InitRoutes(ref WebApplication app)
        {
            #region API Routes
            /// <summary>
            /// /v1.1/CurrentRouteVersion
            /// </summary>
            app.MapGet("/v{version:apiVersion}/CurrentRouteVersion", () =>
            {
                return CurrentRouteVersion();

            }).WithTags(_tags).WithName(string.Format("/{0}/CurrentRouteVersion", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV11);
            #endregion
        }

        public static CurrentApiRoute CurrentRouteVersion()
        {
            return new CurrentApiRoute();
        }

        /// <summary>
        /// Defines the latest version API scheme when queried (returns dev when dev endpoints enabled)
        /// </summary>
        public struct CurrentApiRoute
        {
            public string api { get => "v" + ApiEnvironment.ApiRouteVersions[^1].ToString("f1"); }
        }
    }
}