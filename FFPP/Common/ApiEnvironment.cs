using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.EntityFrameworkCore;
using FFPP.Data;
using FFPP.Data.Logging;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IO;
using System.IO.Pipes;
using FFPP.Api.v10.Tenants;

namespace FFPP.Common
{
    /// <summary>
    /// This class stores Environment variables and constants
    /// </summary>
    public static class ApiEnvironment
    {
        public enum ProductionSecretStores { EncryptedFile };
        // Roles for managing permissions
        public static readonly string RoleOwner = "owner";
        public static readonly string RoleAdmin = "admin";
        public static readonly string RoleEditor = "editor";
        public static readonly string RoleReader = "reader";
#if DEBUG
        public static readonly bool IsDebug = true;
#else
        public static readonly bool IsDebug = false;
#endif
        public static readonly string WorkingDir = Directory.GetCurrentDirectory();
        public static readonly string DataDir = $"{WorkingDir}/Data";
        public static string CacheDir = $"{DataDir}/Cache";
        public static string PersistentDir = "./";
        public static readonly string PreFetchDir = $"{CacheDir}/Pre-Fetch";
        public static readonly string UsersPreFetchDir = $"{PreFetchDir}/Users";
        public static readonly string CachedTenantsFile = $"{CacheDir}/tenants.cache.json";
        public static readonly string LicenseConversionTableFile = $"{DataDir}/ConversionTable.csv";
        public static readonly string LicenseConversionTableMisfitsFile = $"{DataDir}/ConversionTableMisfits.csv";
        public static readonly string ApiVersionFile = $"{WorkingDir}/version_latest.txt";
        public static string WebRootPath = $"{WorkingDir}/wwwroot";
        public static readonly string ApiBinaryVersion = File.ReadAllText(ApiVersionFile);
        public static readonly string ApiHeader = "api";
        public static readonly string ApiAccessScope = "ffpp-api.access";
        public static readonly string FfppSimulatedAuthUsername = "FFPP Simulated Authentication";
        public static string FfppFrontEndUri = "http://localhost";
        public static string MysqlUser = "ffppapiservice";
        public static string MysqlPassword = "wellknownpassword";
        public static string MysqlServer = "localhost";
        public static string MysqlServerPort = "7704";
        public static List<double> ApiRouteVersions = new(){1.0};
        public static ApiVersionSet? ApiVersionSet { get; set; }
        public static readonly ApiVersion ApiDev = new(1.1);
        public static readonly ApiVersion ApiV10 = new(ApiRouteVersions[0]);
        public static readonly ApiVersion ApiV11 = ApiDev;
        public static readonly ApiVersion ApiCurrent = new(ApiRouteVersions[^1]);
        public static readonly string RemoteFfppVersion = "https://raw.githubusercontent.com/White-Knight-IT/FFPP/main/FFPP/version_latest.txt";
        public static readonly DateTime Started = DateTime.UtcNow;
        public static readonly int DbBackoffMs = 20;
        public static bool SimulateAuthenticated = false;
        public static bool ShowDevEnvEndpoints = false;
        public static bool ShowSwaggerUi = false;
        public static bool RunSwagger = false;
        public static bool ServeStaticFiles = false;
        public static bool UseHttpsRedirect = true;
        public static bool HasCredentials = false;
        public static string? DeviceTag = string.Empty;

        /// <summary>
        /// Build data directories including cache directories if they don't exist
        /// </summary>
        public static void DataAndCacheDirectoriesBuild()
        {
            Directory.CreateDirectory(UsersPreFetchDir);
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(PersistentDir);
            Console.WriteLine($"Cache Directory: {CacheDir}");
            Console.WriteLine($"Data Directory: {DataDir}");
            Console.WriteLine($"Persistent Directory: {PersistentDir}");
        }

        /// <summary>
        /// Gets the Api version
        /// </summary>
        /// <returns>Api version object</returns>
        public static FfppVersion GetApiBinaryVersion()
        {
            return new(File.ReadLines(ApiVersionFile).First());
        }

        /// <summary>
        /// Gets a unique 32 byte Device ID
        /// </summary>
        /// <returns>DeviceId as byte[32] array</returns>
        public static async Task<byte[]> GetDeviceId()
        {
            byte[] hmacSalt = UTF8Encoding.UTF8.GetBytes($"ffppDevId{await GetDeviceIdTokenSeed()}seedBytes");

            try
            {
                byte[] hashyBytes = HMACSHA512.HashData(hmacSalt, await GetEntropyBytes());

                // key strech the device id using 173028 HMACSHA512 iterations
                for (int i=0; i<173028; i++)
                {
                    hashyBytes = HMACSHA512.HashData(hmacSalt,hashyBytes);
                }

                // Final hash reduces bytes to byte[32] array perfect for use as AES256 key
                return HMACSHA256.HashData(hmacSalt,hashyBytes);
            }
            catch(Exception ex)
            {
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = $"Exception GetDeviceId: {ex.Message}",
                    Username = "FFPP",
                    Severity = "Error",
                    API = "GetDeviceId"
                });

                throw ex;
            }
        }

        /// <summary>
        /// Shuts down the API (terminates)
        /// </summary>
        /// <param name="error"></param>
        public static void ShutDownApi(int error = 0)
        {
            System.Environment.Exit(error);
        }

        /// <summary>
        /// Update DB with latest migrations
        /// </summary>
        public static async Task<bool> UpdateDbContexts()
        {
            try
            {
                using (FfppLogsDbContext logsDb = new())
                {
                    await logsDb.Database.MigrateAsync();
                }

                using (ExcludedTenantsDbContext exTenantsDb = new())
                {
                    await exTenantsDb.Database.MigrateAsync();
                }

                using (UserProfilesDbContext userProfilesDb = new())
                {
                    await userProfilesDb.Database.MigrateAsync();
                }

                return true;
            }
            catch
            {
                Console.WriteLine($"Didn't create DB tables, this is expected if they already exist - server: {ApiEnvironment.MysqlServer} - port: {ApiEnvironment.MysqlServerPort}");
            }

            return false;
        }

        public static async Task<byte[]> GetEntropyBytes()
        {
            string entropyBytesPath = $"{PersistentDir}/unique.entropy.bytes";
            if (!File.Exists(entropyBytesPath))
            {
                await File.WriteAllBytesAsync(entropyBytesPath, UTF8Encoding.UTF8.GetBytes(Utilities.RandomByteString()));
            }

            return await File.ReadAllBytesAsync(entropyBytesPath);
        }

        public static async Task<bool> CheckForBootstrap()
        {
            string bootstrapPath = $"{PersistentDir}/bootstrap.json";

            // Bootstrap file exists and we don't already have an app password
            if (File.Exists(bootstrapPath))
            {
                Console.WriteLine($"Found bootstrap.json at {bootstrapPath}");
                JsonElement result = await Utilities.ReadJsonFromFile<JsonElement>(bootstrapPath);
                ApiEnvironment.Secrets.TenantId = result.GetProperty("TenantId").GetString();
                ApiEnvironment.Secrets.ApplicationId = result.GetProperty("ApplicationId").GetString();
                ApiEnvironment.Secrets.ApplicationSecret = result.GetProperty("ApplicationSecret").GetString();
                await File.WriteAllTextAsync(bootstrapPath, Utilities.RandomByteString(1024));
                File.Delete(bootstrapPath);
                await ApiZeroConfiguration.Setup(ApiEnvironment.Secrets.TenantId);

                return true;
            }

            return false;
        }

        public static async Task<string> GetDeviceTag()
        {
            return (await ApiEnvironment.GetDeviceIdTokenSeed())[^6..];
        }

        // Gets the DeviceIdTokenSeed used as static entropy in DeviceId generation
        private static async Task<string> GetDeviceIdTokenSeed()
        {
            try
            {
                string deviceTokenPath = $"{PersistentDir}/device.id.token";

                if (!File.Exists(deviceTokenPath))
                {
                    await File.WriteAllTextAsync(deviceTokenPath, Guid.NewGuid().ToString());
                }

                return (await File.ReadAllTextAsync(deviceTokenPath)).TrimEnd('\n').Trim();
            }
            catch(Exception ex)
            {
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = $"Exception GetDeviceIdToken: {ex.Message}",
                    Username = "FFPP",
                    Severity = "Error",
                    API = "GetDeviceIdTokenSeed"
                });

                throw ex;
            }
        }

        /// <summary>
        /// Used to define a Version structure for use in the api
        /// </summary>
        public struct FfppVersion
        {
            public FfppVersion(string rawVersion)
            {
                RawVersion = rawVersion;
                Version = Version.Parse(rawVersion.Split(":")[0]);
                DisplayVersion = string.Format("{0}-{1} ({2})", Version, RawVersion.Split(":")[1], RawVersion.Split(":")[2]);
            }

            public string RawVersion { get; set; }
            public Version Version { get; set; }
            public string DisplayVersion { get; set; }
        }

        /// <summary>
        /// This is used to store the secrets that we will retrieve from user-secrets in dev, or a key vault in prod.
        /// </summary>
        public static class Secrets
        {
            public static string? ApplicationId { get; set; }
            public static string? ApplicationSecret { get; set; }
            public static string? TenantId { get; set; }
            public static string? RefreshToken { get; set; }
            public static string? ExchangeRefreshToken { get; set; }
        }
    }

}

