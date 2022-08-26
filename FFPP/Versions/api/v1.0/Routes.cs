using Microsoft.Identity.Web.Resource;
using System.Security.Claims;
using FFPP.Common;
using FFPP.Data;
using FFPP.Api.v10.Dashboards;
using FFPP.Api.v10.Users;
using FFPP.Api.v10.Tenants;

namespace FFPP.Api.v10
{
    /// <summary>
    /// /v1.0 ### THIS IS THE V1.0 ENDPOINTS ###
    /// </summary>
    public static class Routes
    {
        private static readonly string _versionHeader = "v1.0";

        public static void InitRoutes(ref WebApplication app)
        {
            /// <summary>
            /// /v1.0/.auth/me
            /// </summary>
            app.MapGet("/v{version:apiVersion}/.auth/me", async (HttpContext context, HttpRequest request) =>
            {
                try
                {
                    return await AuthMe(context);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }

            }).WithName(string.Format("/{0}/.auth/me", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/CurrentRouteVersion
            /// </summary>
            app.MapGet("/v{version:apiVersion}/CurrentRouteVersion", async (HttpContext context) =>
            {
                try
                {
                    return await CurrentRouteVersion();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }

            }).WithName(string.Format("/{0}/CurrentRouteVersion", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);


            /// <summary>
            /// /v1.0//EditUserProfile
            /// </summary>
            app.MapPut("/v{version:apiVersion}/EditUserProfile", async (HttpContext context, UserProfilesDbContext.UserProfile inputProfile, bool? updatePhoto) =>
            {
                try
                {
                    return await UpdateUserProfile(context, inputProfile, updatePhoto ?? false);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }

            }).WithName(string.Format("/{0}/EditUserProfile", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/GetDashboard
            /// </summary>
            app.MapGet("/v{version:apiVersion}/GetDashboard", async (HttpContext context, HttpRequest request) =>
            {
                try
                {
                    return await GetDashboard(context);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/GetDashboard", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/GetVersion
            /// </summary>
            app.MapGet("/v{version:apiVersion}/GetVersion", async (HttpContext context, HttpRequest request, string LocalVersion) =>
            {
                try
                {
                    return await GetVersion(context, LocalVersion);
                }
                catch(UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/GetVersion", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/Heartbeat
            /// </summary>
            app.MapGet("/v{version:apiVersion}/Heartbeat", async (HttpContext context) =>
            {
                try
                {
                    Task<object> task = new(() =>
                    {
                        return new Heartbeat();
                    });

                    task.Start();

                    return await task;
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/Heartbeat", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListDomains
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListDomains", async (HttpContext context, HttpRequest request, string TenantFilter) =>
            {
                try
                {
                    return await ListDomains(context, TenantFilter);
                }              
                catch(UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/ListDomains", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListSites
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListSites", async (HttpContext context, HttpRequest request, string TenantFilter, string Type, string? UserUPN) =>
            {
                try
                {
                    return await ListSites(context, TenantFilter, Type, UserUPN ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }

            }).WithName(string.Format("/{0}/ListSites", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListTenants
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListTenants", async (HttpContext context, HttpRequest request, bool ? AllTenantSelector) =>
            {
                try
                {
                    return await ListTenants(context, AllTenantSelector ?? false);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }

            }).WithName(string.Format("/{0}/ListTenants", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListUsers
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListUsers", async (HttpContext context, HttpRequest request, string TenantFilter, string? UserId) =>
            {
                try
                {
                    return await ListUsers(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/ListUsers", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListUserConditionalAccessPolicies
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListUserConditionalAccessPolicies", async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                { 
                    return await ListUserConditionalAccessPolicies(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/ListUserConditionalAccessPolicies", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListUserDevices
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListUserDevices", async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            { 
                try
                {
                    return await ListUserDevices(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/ListUserDevices", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListUserGroups
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListUserGroups", async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await ListUserGroups(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/ListUserGroups", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListUserMailboxDetails
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListUserMailboxDetails", async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await ListUserMailboxDetails(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/ListUserMailboxDetails", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListUserPhoto
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListUserPhoto", async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await ListUserPhoto(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/ListUserPhoto", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);

            /// <summary>
            /// /v1.0/ListUserSigninLogs
            /// </summary>
            app.MapGet("/v{version:apiVersion}/ListUserSigninLogs", async (HttpContext context, HttpRequest request, string TenantFilter, string UserId) =>
            {
                try
                {
                    return await ListUserSigninLogs(context, TenantFilter, UserId ?? string.Empty);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = 401;
                    return Results.Unauthorized();
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    return Results.Problem();
                }
            }).WithName(string.Format("/{0}/ListUserSigninLogs", _versionHeader)).WithApiVersionSet(ApiEnvironment.ApiVersionSet).MapToApiVersion(ApiEnvironment.ApiV10);
        }

        public static async Task<object> CurrentRouteVersion()
        {
            Task<CurrentApiRoute> task = new(() =>
            {
                return new CurrentApiRoute();
            });

            task.Start();

            return await task;
        
        }

        public static async Task<object> AuthMe(HttpContext context)
        {
            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
            }

            Task<Auth> task = new(() =>
            { 
               List<string> roles = new();

               // I think we can only have one role but I'll iterate just in case it happens
               foreach (Claim c in context.User.Claims.Where(x => x.Type.ToLower().Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")).ToList())
               {
                   roles.Add(c.Value);
               }
                try
                {
                    return new Auth()
                    {
                        clientPrincipal = new()
                        {
                            userId = Guid.Parse(context.User.Claims.First(x => x.Type.ToLower().Equals("http://schemas.microsoft.com/identity/claims/objectidentifier")).Value),
                            identityProvider = "aad",
                            name = context.User.Claims.First(x => x.Type.ToLower().Equals("name")).Value,
                            userDetails = context.User.Claims.First(x => x.Type.ToLower().Equals("preferred_username")).Value,
                            userRoles = roles
                        }
                    };
                }
                catch
                {
                    context.Response.StatusCode = 400;
                    return new Auth();
                }
            });

            task.Start();

            Auth authUserProfile =  await task;

            authUserProfile.clientPrincipal.photoData = await User.GetUserPhoto(authUserProfile.clientPrincipal.userId.ToString(), UserPhotoSize.Small, context.User.Claims.First(x => x.Type.ToLower().Contains("tenantid")).Value);
            authUserProfile.clientPrincipal.defaultUseageLocation = await User.GetUserUseageLocation(context.User.Claims.First(x => x.Type.ToLower().Equals("http://schemas.microsoft.com/identity/claims/tenantid")).Value, authUserProfile.clientPrincipal.userId.ToString());

            // Check if profile exists, update and use if it does, create and use if it doesn't

            UserProfilesDbContext.UserProfile? userDbProfile = await UserProfilesDbThreadSafeCoordinator.ThreadSafeGetUserProfile(authUserProfile.clientPrincipal.userId);

            // User has no profile so we will create it
            if (userDbProfile == null)
            {
                authUserProfile.clientPrincipal.theme = "dark";
                authUserProfile.clientPrincipal.lastTenantName = "* All Tenants";
                authUserProfile.clientPrincipal.lastTenantDomainName = "AllTenants";
                authUserProfile.clientPrincipal.lastTenantCustomerId = "AllTenants";
                authUserProfile.clientPrincipal.defaultPageSize = 100;
                UserProfilesDbThreadSafeCoordinator.ThreadSafeAdd(authUserProfile.clientPrincipal);
                return authUserProfile;
            }

            // User exists in the DB yay let us use it
            authUserProfile.clientPrincipal.theme = userDbProfile.theme;
            authUserProfile.clientPrincipal.lastTenantName = userDbProfile.lastTenantName;
            authUserProfile.clientPrincipal.lastTenantDomainName = userDbProfile.lastTenantDomainName;
            authUserProfile.clientPrincipal.lastTenantCustomerId = userDbProfile.lastTenantCustomerId;
            authUserProfile.clientPrincipal.defaultPageSize = userDbProfile.defaultPageSize;
            UserProfilesDbThreadSafeCoordinator.ThreadSafeAdd(authUserProfile.clientPrincipal);
            return authUserProfile;
        }

        public static async Task<object> UpdateUserProfile(HttpContext context, UserProfilesDbContext.UserProfile inputProfile, bool updatePhoto)
        {
            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
            }

            // Make sure that the users auth token matches the user who's profile they are trying to update
            if(Guid.Parse(context.User.Claims.First(x => x.Type.ToLower().Equals("http://schemas.microsoft.com/identity/claims/objectidentifier")).Value) != inputProfile.userId)            
            {
                throw new UnauthorizedAccessException();
            }

            try
            {
                List<string> roles = new();

                // I think we can only have one role but I'll iterate just in case it happens
                foreach (Claim c in context.User.Claims.Where(x => x.Type.ToLower().Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")).ToList())
                {
                    roles.Add(c.Value);
                }

                inputProfile.identityProvider = "aad";
                inputProfile.name = context.User.Claims.First(x => x.Type.ToLower().Equals("name")).Value;
                inputProfile.userDetails = context.User.Claims.First(x => x.Type.ToLower().Equals("preferred_username")).Value;
                inputProfile.userRoles = roles;
                // We apply sensible defaults below if we are given null for values
                inputProfile.defaultPageSize ??= 100;
                inputProfile.lastTenantCustomerId ??= "AllTenants";
                inputProfile.lastTenantDomainName ??= "AllTenants";
                inputProfile.lastTenantName ??= "* All Tenants";
                inputProfile.theme ??= "dark";
                    
                await UserProfilesDbThreadSafeCoordinator.ThreadSafeUpdateUserProfile(inputProfile, updatePhoto);
                
                return true;
            }
            catch
            {
               
            }

            return false;
        }

        public static async Task<object> GetDashboard(HttpContext context)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await FfppDashboard.GetHomeData(accessingUser);
        }

        public static async Task<object>GetVersion(HttpContext context, string LocalVersion)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await FfppDashboard.CheckVersions(accessingUser, LocalVersion);
        }

        public static async Task<object> ListSites(HttpContext context, string tenantFilter, string type, string userUpn)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await User.GetSites(tenantFilter, type, userUpn, accessingUser);
        }

        public static async Task<object> ListTenants(HttpContext context, bool? AllTenantSelector)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await Tenant.GetTenants(accessingUser, allTenantSelector: AllTenantSelector ?? false);
        }

        public static async Task<object> ListUserConditionalAccessPolicies(HttpContext context, string TenantFilter, string UserId)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await User.GetUserConditionalAccessPolicies(TenantFilter, UserId ?? string.Empty, accessingUser);
        }

        public static async Task<object> ListUserDevices(HttpContext context, string TenantFilter, string UserId)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await User.GetUserDevices(TenantFilter, UserId ?? string.Empty, accessingUser);
        }

        public static async Task<object> ListUserGroups(HttpContext context, string TenantFilter, string UserId)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await User.GetUserGroups(TenantFilter, UserId ?? string.Empty, accessingUser);
        }

        public static async Task<object> ListUserMailboxDetails(HttpContext context, string TenantFilter, string UserId)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await User.GetUserMailboxDetails(TenantFilter, UserId ?? string.Empty, accessingUser);
        }

        public static async Task<object> ListUserPhoto(HttpContext context, string TenantFilter, string UserId)
        {
            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
            }
            
            return await User.GetUserPhoto(UserId, UserPhotoSize.Small, TenantFilter);
        }

        public static async Task<object> ListUserSigninLogs(HttpContext context, string TenantFilter, string UserId)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await User.GetUserSigninLogs(TenantFilter, UserId ?? string.Empty, accessingUser);
        }

        public static async Task<object> ListUsers(HttpContext context, string TenantFilter, string UserId)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await User.GetUsers(accessingUser, TenantFilter, UserId ?? string.Empty);
        }

        public static async Task<object> ListDomains(HttpContext context, string TenantFilter)
        {
            string accessingUser = ApiEnvironment.FfppSimulatedAuthUsername;

            if (!ApiEnvironment.SimulateAuthenticated)
            {
                CheckUserIsReader(context);
                accessingUser = await Utilities.UsernameParse(context);
            }

            return await Domain.GetDomains(accessingUser, TenantFilter);
        }

        private static void CheckUserIsReader(HttpContext context)
        {
            string[] scopes = { ApiEnvironment.ApiAccessScope };
            string[] roles = { "owner", "admin", "editor", "reader" };
            context.ValidateAppRole(roles);
            context.VerifyUserHasAnyAcceptedScope(scopes);
        }

        private static void CheckUserIsEditor(HttpContext context)
        {
            string[] scopes = { ApiEnvironment.ApiAccessScope };
            string[] roles = { "owner", "admin", "editor" };
            context.ValidateAppRole(roles);
            context.VerifyUserHasAnyAcceptedScope(scopes);
        }

        private static void CheckUserIsAdmin(HttpContext context)
        {
            string[] scopes = { ApiEnvironment.ApiAccessScope };
            string[] roles = { "owner", "admin" };
            context.ValidateAppRole(roles);
            context.VerifyUserHasAnyAcceptedScope(scopes);
        }

        private static void CheckUserIsOwner(HttpContext context)
        {
            string[] scopes = { ApiEnvironment.ApiAccessScope };
            string[] roles = { "owner" };
            context.ValidateAppRole(roles);
            context.VerifyUserHasAnyAcceptedScope(scopes);
        }

        /// <summary>
        /// Defines a ClientPrincipal returned when /.auth/me is called
        /// </summary>
        public struct Auth
        {
            public UserProfilesDbContext.UserProfile clientPrincipal { get; set; }
        }



        /// <summary>
        /// Defines a Heartbeat object we return when the /api/Heartbeat API is polled
        /// </summary>
        public struct Heartbeat
        {
            public DateTime started { get => ApiEnvironment.Started; }
            public long errorsSinceStarted { get => ApiEnvironment.RunErrorCount; }
            public bool? isBootstrapped { get => ApiEnvironment.IsBoostrapped; }
        }

        /// <summary>
        /// Defines the latest version API scheme when /api/CurrentApiRoute queried (returns dev when dev endpoints enabled)
        /// </summary>
        public struct CurrentApiRoute
        {
            public string api { get => "v" + ApiEnvironment.ApiRouteVersions[^1].ToString("f1"); }
        }

        /// <summary>
        /// Defines the error we send back in JSON payload
        /// </summary>
        public struct ErrorResponseBody
        {
            public int errorCode { get; set; }
            public string message { get; set; }
        }
    }
}

