using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FFPP.Common;
using FFPP.Data.Logging;
using static FFPP.Data.UserProfilesDbContext;

namespace FFPP.Data
{
    /// <summary>
    /// Entity Framework Class used to create and manage UserProfiles in a DB
    /// </summary>
    public class UserProfilesDbContext : DbContext
    {
        private DbSet<UserProfile>? _userProfiles { get; set; }

        public UserProfilesDbContext()
        {

        }

        public async Task<bool> AddUserProfile(UserProfile user)
        {
            Task<bool> addTask = new(() =>
            {
                try
                {
                    if (!ExistsById(user.userId).Result)
                    {
                        Add(user);
                        SaveChanges();
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    ApiEnvironment.RunErrorCount++;
                    Console.WriteLine($"Exception writing  in UserProfiles: {ex.Message}");
                    throw;
                }
            });

            addTask.Start();
            return await addTask;
        }



        public async Task<bool> ExistsById(Guid userId)
        {
                if (await _userProfiles.FindAsync(userId) == null)
                {
                    return false;
                }

                return true;
        }

        public async Task<UserProfile>? GetById(Guid userId)
        {
            try
            {
                return await _userProfiles.FindAsync(userId);
            }
            catch
            {

            }

            return null;
        }

        public async Task<bool> UpdateUserProfile(UserProfile userProfile, bool updatePhoto=true)
        {
            try
            {
                UserProfile? foundUser = await _userProfiles.FindAsync(userProfile.userId);

                if (foundUser != null)
                {
                    if (updatePhoto)
                    {
                        foundUser.photoData = userProfile.photoData;
                    }

                    foundUser.name = userProfile.name;
                    foundUser.defaultPageSize = userProfile.defaultPageSize;
                    foundUser.defaultUseageLocation = userProfile.defaultUseageLocation;
                    foundUser.identityProvider = userProfile.identityProvider;
                    foundUser.lastTenantCustomerId = userProfile.lastTenantCustomerId;
                    foundUser.lastTenantDomainName = userProfile.lastTenantDomainName;
                    foundUser.lastTenantName = userProfile.lastTenantName;
                    foundUser.theme = userProfile.theme;
                    foundUser.userDetails = userProfile.userDetails;
                    SaveChanges();

                    return true;
                }
            }
            catch(Exception ex)
            {
                ApiEnvironment.RunErrorCount++;

                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = $"Error updating user profile for {userProfile.userId.ToString()} - {userProfile.name}: {ex.Message}",
                    Username = "FFPP",
                    Severity = "Error",
                    API = "UpdateUserProfile"
                });
            }

            return false;
        }

        private async Task<bool> Exists(Guid userId)
        {
            if (await _userProfiles.FindAsync(userId) == null)
            {
                return false;
            }

            return true;
        }

        // Tells EF that we want to use MySQL
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string connectionString = $"server={ApiEnvironment.MysqlServer};database=ffpp;user={ApiEnvironment.MysqlUser};password={ApiEnvironment.MysqlPassword};port={ApiEnvironment.MysqlServerPort}";
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        /// <summary>
        /// Represents a UserProfile object as it exists in the UserProfiles DB
        /// </summary>
        public class UserProfile
        {
            [Key] // Public key
            public Guid userId { get; set; }
            public string? identityProvider { get; set; }
            public string? name { get; set; }
            public string? userDetails { get; set; }
            [NotMapped] // We never save roles they may change and relying on old roles is security risk
            public List<string>? userRoles { get; set; }
            public string? theme { get; set; }
            public int? defaultPageSize { get; set; }
            public string? defaultUseageLocation { get; set; }
            public string? lastTenantName { get; set; }
            public string? lastTenantDomainName { get; set; }
            public string? lastTenantCustomerId { get; set; }
            public string? photoData { get; set; }
        }
    }

    /// <summary>
    /// A class for accessing the UserProfilesDbContext in a thread safe manner
    /// </summary>
    public static class UserProfilesDbThreadSafeCoordinator
    {
        private static bool _locked = false;

        /// <summary>
        /// Thread safe means of updating a user profile
        /// </summary>
        /// <param name="userProfile">The user profile to update in DB</param>
        /// <param name="updatePhoto">Bool to allow/reject updating of photo</param>
        /// <returns>bool indicating success</returns>
        public static async Task<bool> ThreadSafeUpdateUserProfile(UserProfilesDbContext.UserProfile userProfile, bool updatePhoto)
        {
            WaitForUnlock();

            _locked = true;

            Task<bool>? updateUserProfile = new(() =>
            {
                using (UserProfilesDbContext userProfiles = new())
                {
                    return userProfiles.UpdateUserProfile(userProfile, updatePhoto).Result;
                }
            });

            return await ExecuteQuery<bool>(updateUserProfile);
        }

        /// <summary>
        /// Thread safe means to get a User Profile from DB
        /// </summary>
        /// <param name="userId">User ID of the user profile to return</param>
        /// <returns>User profile if it exists else null</returns>
        public static async Task<UserProfilesDbContext.UserProfile>? ThreadSafeGetUserProfile(Guid userId)
        {
            WaitForUnlock();

            _locked = true;

            Task<UserProfilesDbContext.UserProfile>? getUserProfile = new(() =>
            {
                using (UserProfilesDbContext userProfiles = new())
                {
                    return userProfiles.GetById(userId).Result;
                }
            });

            return await ExecuteQuery<UserProfilesDbContext.UserProfile>(getUserProfile);
        }

        /// <summary>
        /// Add a tenant to the ExcludedTenantsDbContext in a thread safe manner
        /// </summary>
        /// <param name="excludedTenant">tenant to add to DB</param>
        /// <returns>bool indicating success</returns>
        public static async Task<bool> ThreadSafeAdd(UserProfilesDbContext.UserProfile userProfile)
        {
            WaitForUnlock();

            _locked = true;

            Task<bool> addUserProfile = new(() =>
            {
                using (UserProfilesDbContext userProfiles = new())
                {
                    return userProfiles.AddUserProfile(userProfile).Result;
                }
            });

            return await ExecuteQuery<bool>(addUserProfile);
        }

        private static async Task<type> ExecuteQuery<type>(Task<type> taskToRun)
        {
            try
            {
                taskToRun.Start();
                return (type)await taskToRun;
            }
            catch
            {
                // We make sure we unlock when an exception occurs as to not end up in a perpetually locked state
                _locked = false;
                throw;
            }
            finally
            {
                _locked = false;
            }
        }

        // Blocking wait for DB context to become unlocked
        private static void WaitForUnlock()
        {
            while (_locked)
            {
                Thread.CurrentThread.Join(ApiEnvironment.DbBackoffMs);
            }
        }
    }
}

