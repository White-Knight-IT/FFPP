using CurrentApi = FFPP.Api.v10;
using FFPP.Data;
using FFPP.Common;

namespace FFPP.Api
{
    /// <summary>
    /// /api is basically a skeleton set of routes that always point to the latest /vX.Y api
    /// </summary>
    public static class Routes
    {
        public static void InitRoutes(ref WebApplication app)
        {
            #region API Routes

            /// <summary>
            /// /.auth/me
            /// </summary>
            app.MapGet("/.auth/me", async (HttpContext context, HttpRequest request) =>
            {
                try
                {
                    return await CurrentApi.Routes.AuthMe(context);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }

            }).WithName("/.auth/me").ExcludeFromDescription();

            /// <summary>
            /// /api/.auth/me
            /// </summary>
            app.MapGet(string.Format("/{0}/.auth/me", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request) =>
            {
                try
                {
                    return await CurrentApi.Routes.AuthMe(context);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }

            }).WithName(string.Format("/{0}/.auth/me", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/CurrentRouteVersion
            /// </summary>
            app.MapGet(string.Format("/{0}/CurrentRouteVersion", ApiEnvironment.ApiHeader), async () =>
            {
                return await CurrentApi.Routes.CurrentRouteVersion();

            }).WithName(string.Format("/{0}/CurrentRouteVersion", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/EditUserProfile
            /// </summary>
            app.MapPut(string.Format("/{0}/EditUserProfile", ApiEnvironment.ApiHeader), async (HttpContext context, UserProfilesDbContext.UserProfile inputProfile, bool? updatePhoto) =>
            {
                try
                {
                    return await CurrentApi.Routes.UpdateUserProfile(context, inputProfile, updatePhoto ?? false);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }

            }).WithName(string.Format("/{0}/EditUserProfile", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/GetDashboard
            /// </summary>
            app.MapGet(string.Format("/{0}/GetDashboard", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request) =>
            {
                try
                {
                    return await CurrentApi.Routes.GetDashboard(context);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/GetDashboard", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/GetVersion
            /// </summary>
            app.MapGet(string.Format("/{0}/GetVersion", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string LocalVersion) =>
            {
                try
                {
                    return await CurrentApi.Routes.GetVersion(context, LocalVersion);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/GetVersion", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/Heartbeat
            /// </summary>
            app.MapGet(string.Format("/{0}/Heartbeat", ApiEnvironment.ApiHeader), async () =>
            {
                Task<CurrentApi.Routes.Heartbeat> task = new(() =>
                {
                    return new CurrentApi.Routes.Heartbeat();
                });

                task.Start();

                return await task;
            })
            .WithName(string.Format("/{0}/Heartbeat", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListDomains
            /// </summary>
            app.MapGet(string.Format("/{0}/ListDomains", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string TenantFilter) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListDomains(context, TenantFilter);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/ListDomains", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListSites
            /// </summary>
            app.MapGet("/{0}/ListSites", async (HttpContext context, HttpRequest request, string TenantFilter, string Type, string? UserUPN) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListSites(context, TenantFilter, Type, UserUPN ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }

            })
            .WithName(string.Format("/{0}/ListSites", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListTenants
            /// </summary>
            app.MapGet(string.Format("/{0}/ListTenants", ApiEnvironment.ApiHeader) , async (HttpContext context, HttpRequest request, bool? AllTenantSelector) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListTenants(context, AllTenantSelector ?? false);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/ListTenants", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListUsers
            /// </summary>
            app.MapGet(string.Format("/{0}/ListUsers", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string TenantFilter, string? UserId) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListUsers(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/ListUsers", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListUserConditionalAccessPolicies
            /// </summary>
            app.MapGet(string.Format("/{0}/ListUserConditionalAccessPolicies", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListUserConditionalAccessPolicies(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/ListUserConditionalAccessPolicies", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListUserDevices
            /// </summary>
            app.MapGet(string.Format("/{0}/ListUserDevices", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListUserDevices(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/ListUserDevices", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListUserGroups
            /// </summary>
            app.MapGet(string.Format("/{0}/ListUserGroups", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListUserGroups(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            }).WithName(string.Format("/{0}/ListUserGroups", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListUserMailboxDetails
            /// </summary>
            app.MapGet(string.Format("/{0}/ListUserMailboxDetails", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListUserMailboxDetails(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/ListUserMailboxDetails", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListUserPhoto
            /// </summary>
            app.MapGet(string.Format("/{0}/ListUserPhoto", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListUserPhoto(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            })
            .WithName(string.Format("/{0}/ListUserPhoto", ApiEnvironment.ApiHeader)).ExcludeFromDescription();

            /// <summary>
            /// /api/ListUserSigninLogs
            /// </summary>
            app.MapGet(string.Format("/{0}/ListUserSigninLogs", ApiEnvironment.ApiHeader), async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await CurrentApi.Routes.ListUserSigninLogs(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
            }).WithName(string.Format("/{0}/ListUserSigninLogs", ApiEnvironment.ApiHeader)).ExcludeFromDescription();
            #endregion
        }
    }
}