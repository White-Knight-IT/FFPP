using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FFPP.Common;
using Microsoft.AspNetCore.Http;
using static FFPP.Data.ExcludedTenantsDbContext;

namespace FFPP.Data
{
    /// <summary>
    /// Entity Framework Class used to create and manage ExcludedTenants in a DB
    /// </summary>
    public class ExcludedTenantsDbContext : DbContext
    {
        private DbSet<ExcludedTenant>? _excludedTenantEntries { get; set; }

        public ExcludedTenantsDbContext()
        {

        }

        public async Task<bool> Exists(string defaultDomainName)
        {
            if (await _excludedTenantEntries.FindAsync(defaultDomainName) == null)
            {
                return false;
            }

            return true;
        }

        public async Task<List<ExcludedTenant>> List()
        {
            return await _excludedTenantEntries.ToListAsync() ?? new();
        }

        public async Task<bool> AddExcludedTenant(ExcludedTenant exclude)
        {
            try
            {
                Task<bool> task = new(() =>
                {
                    exclude.DateString = DateTime.Now.ToString("dd-MM-yyyy");

                    if (string.IsNullOrEmpty(exclude.Username))
                    {
                        exclude.Username = "FFPP";
                    }

                    int repeatOnFail = 5;
                    int attempts = 1;

                    do
                    {
                        try
                        {
                            if (_excludedTenantEntries.FindAsync(exclude.TenantDefaultDomain).Result == null)
                            {
                                Add(exclude);
                                SaveChanges();
                                attempts = repeatOnFail + 1;
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Thread.CurrentThread.Join(attempts * ApiEnvironment.DbBackoffMs); // Sleep a multiple of a 5th of a second each attempt
                            attempts++;

                            if (attempts > repeatOnFail)
                            {
                                throw ex;
                            }
                        }

                    }
                    while (attempts <= repeatOnFail);

                    return false;
                });

                task.Start();

                return await task;
            }
            catch (Exception ex)
            {
                ApiEnvironment.RunErrorCount++;
                Console.WriteLine($"Exception writing  in ExcludedTenant: {ex.Message}");
                throw ex;
            }
        }

        // Tells EF that we want to use MySQL
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string connectionString = $"server={ApiEnvironment.MysqlServer};database=ffpp;user={ApiEnvironment.MysqlUser};password={ApiEnvironment.MysqlPassword};port={ApiEnvironment.MysqlServerPort}";
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        // Represents an ExcludedTenant object as it exists in the ExcludedTenants DB
        public class ExcludedTenant
        {
            [Key] // Public key
            public string? TenantDefaultDomain { get; set; }
            public string? DateString { get; set; }
            public string? Username { get; set; }
        }
    }

    /// <summary>
    /// A class for accessing the ExcludedTenantsDbContext in a thread safe manner
    /// </summary>
    public static class ExcludedTenantsDbThreadSafeCoordinator
    {
        private static bool _locked = false;

        /// <summary>
        /// A thread safe means of checking if a tenant exists in the ExcludedTenantsDbContext
        /// </summary>
        /// <param name="defaultDomainName"></param>
        /// <returns>bool indicates true if tenant resided in DB</returns>
        public static async Task<bool> ThreadSafeTenantIsExcluded(string defaultDomainName)
        {
            WaitForUnlock();

            _locked = true;

            Task<bool> existsTask = new(() =>
            {
                using (ExcludedTenantsDbContext excludedTenantEntries = new())
                {
                    return excludedTenantEntries.Exists(defaultDomainName).Result;
                }
            });

            return await ExecuteQuery<bool>(existsTask);

        }

        /// <summary>
        /// Add a tenant to the ExcludedTenantsDbContext in a thread safe manner
        /// </summary>
        /// <param name="excludedTenant">tenant to add to DB</param>
        /// <returns>bool indicating success</returns>
        public static async Task<bool> ThreadSafeAdd(ExcludedTenantsDbContext.ExcludedTenant excludedTenant)
        {
            WaitForUnlock();

            _locked = true;

            Task<bool> addExcludedTenant = new(() =>
            {
                using (ExcludedTenantsDbContext excludedTenantEntries = new())
                {
                    return excludedTenantEntries.AddExcludedTenant(excludedTenant).Result;
                }
            });

            return await ExecuteQuery<bool>(addExcludedTenant);

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

