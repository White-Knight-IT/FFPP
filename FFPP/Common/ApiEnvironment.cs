using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.EntityFrameworkCore;
using DeviceId;
using FFPP.Data;
using FFPP.Data.Logging;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IO;
using System.IO.Pipes;
using FFPP.Api.v10.Tenants;
using static FFPP.Data.ExcludedTenantsDbContext;
using static FFPP.Data.UserProfilesDbContext;

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
        public static readonly string DataDir = WorkingDir + "/Data";
        public static string CacheDir = DataDir + "/Cache";
        public static string PersistentDir = WorkingDir;
        public static readonly string PreFetchDir = CacheDir + "/Pre-Fetch";
        public static readonly string UsersPreFetchDir = PreFetchDir + "/Users";
        public static readonly string CachedTenantsFile = CacheDir + "/tenants.cache.json";
        public static readonly string WwwRootDir = WorkingDir + "/wwwroot";
        public static readonly string LicenseConversionTableFile = DataDir + "/ConversionTable.csv";
        public static readonly string LicenseConversionTableMisfitsFile = DataDir + "/ConversionTableMisfits.csv";
        public static readonly string ZeroConfPath = PersistentDir + "/api.zeroconf.json";
        public static readonly string ApiVersionFile = WorkingDir + "/version_latest.txt";
        public static readonly string DeviceTokenPath = PersistentDir + "/device.id.token";
        public static readonly string UniqueEntropyBytesPath = PersistentDir + "/unique.entropy.bytes";
        public static readonly string ApiBinaryVersion = File.ReadAllText(ApiVersionFile);
        public static readonly string ApiHeader = "api";
        public static readonly string ApiAccessScope = "ffpp-api.access";
        public static readonly string FfppSimulatedAuthUsername = "FFPP Simulated Authentication";
        public static string FfppFrontEndUri = "https://localhost:7074";
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
        public static bool ServeStaticFiles= false;
        public static bool UseHttpsRedirect = true;
        public static byte[]? EntropyBytes;
        public static string PlainTextFilePath = PersistentDir + "/plain.secrets.json";
        public static string CipherTextFilePath = PersistentDir + "/encrypted.secrets.json";
        public static bool HasCredentials = false;

        /// <summary>
        /// Build data directories including cache directories if they don't exist
        /// </summary>
        public static void DataAndCacheDirectoriesBuild()
        {
            Directory.CreateDirectory(UsersPreFetchDir);
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
        ///  Checks that the DBs exist and if they don't create them
        /// </summary>
        public static async void CheckCreateDbs()
        {
            
        }

        /// <summary>
        /// Gets a unique Device ID from a combination of hardware identifiers and a stored GUID
        /// (GUID ensures uniqued ID per host on shared hardware)
        /// </summary>
        /// <returns>DeviceId as byte[32] array</returns>
        public static async Task<byte[]> GetDeviceId()
        {
            byte[] hmacSalt = ApiEnvironment.EntropyBytes ?? UTF8Encoding.UTF8.GetBytes(await GetDeviceIdTokenSeed());

            try
            {
                byte[] hashyBytes = HMACSHA512.HashData(hmacSalt,UTF8Encoding.UTF8.GetBytes(new DeviceIdBuilder()
                    .AddFileToken(DeviceTokenPath) // Random entropy makes DeviceId unobtainable by someone without device access
                    .OnWindows(windows => windows
                        .AddProcessorId()
                        .AddMotherboardSerialNumber()
                        .AddSystemDriveSerialNumber())
                    .OnLinux(linux => linux
                        .AddCpuInfo()
                        .AddMotherboardSerialNumber()
                        .AddSystemDriveSerialNumber())
                    .OnMac(mac => mac
                        .AddSystemDriveSerialNumber()
                        .AddPlatformSerialNumber()).ToString()));

                // key strech the device id using 973028 HMACSHA512 iterations
                for (int i=0; i<973028; i++)
                {
                    hashyBytes = HMACSHA512.HashData(hmacSalt, hashyBytes);
                }

                // Final hash reduces bytes to byte[32] array perfect for use as AES256 key
                return HMACSHA256.HashData(hmacSalt,hashyBytes);
            }
            catch(Exception ex)
            {
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = $"Exception GetDeviceId: {ex.Message} Inner Exception: {ex.InnerException.Message ?? string.Empty}",
                    Username = "FFPP",
                    Severity = "Error",
                    API = "GetDeviceId"
                });

                throw ex;
            }
        }

        public static async Task<bool> GetProductionSecrets(ProductionSecretStores store)
        { 
            try
            {
                JsonElement plainSecrets;
                FileStream encryptedSecrets;

                if (!File.Exists(CipherTextFilePath))
                {
                    encryptedSecrets = new FileStream(CipherTextFilePath, FileMode.Create);

                    if (!File.Exists(PlainTextFilePath))
                    {
                        // No encrypted secrets and no plain secrets to ingest, uh oh, sad face
                        throw new Exception("No encrypted secrets file exists, nor any plain secrets file to build encrypted secrets. Please create plain.secrets.json file in Data directory.");
                    }

                    try
                    {
                        string plainSecretsJsonString = await File.ReadAllTextAsync(PlainTextFilePath);
                        // We have a plain secrets file to ingest, encrypt and then dispose of
                         plainSecrets = (JsonElement)JsonSerializer.Deserialize(plainSecretsJsonString, typeof(JsonElement));

                        // Write encrypted secrets file
                        await File.WriteAllTextAsync(CipherTextFilePath, await Utilities.Crypto.AesEncrypt(plainSecretsJsonString, await GetDeviceId()));

                    }
                    catch(Exception ex)
                    {
                        FfppLogsDbContext.DebugConsoleWrite(string.Format("Invalid JSON in plain.secrets.json file! {0} {1}", ex.Message, ex.InnerException.Message ?? string.Empty));
                        throw new Exception(string.Format("Invalid JSON in plain.secrets.json file! {0} {1}",ex.Message, ex.InnerException.Message ?? string.Empty));
                    }

                    // Overwrite plain.secrets.json file
                    await File.WriteAllTextAsync(PlainTextFilePath, Utilities.RandomByteString());

                    //Now delete plain text file
                    File.Delete(PlainTextFilePath);
                }

                // We have encrypted secrets file

                string plain = await Utilities.Crypto.AesDecrypt(await File.ReadAllTextAsync(CipherTextFilePath), await GetDeviceId());
                plainSecrets = (JsonElement)JsonSerializer.Deserialize(plain, typeof(JsonElement));
                Secrets.ApplicationId = plainSecrets.GetProperty("ApplicationId").GetString();
                Secrets.ApplicationSecret = plainSecrets.GetProperty("ApplicationSecret").GetString();
                Secrets.TenantId = plainSecrets.GetProperty("TenantId").GetString();
                Secrets.RefreshToken = plainSecrets.GetProperty("RefreshToken").GetString();
                Secrets.ExchangeRefreshToken = plainSecrets.GetProperty("ExchangeRefreshToken").GetString();
            }
            catch(Exception ex)
            {
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = $"Exception Encrypting or Decrypting Secrets (GetProductionSecrets): {ex.Message} Inner Exception: {ex.InnerException.Message ?? string.Empty}",
                    Username = "FFPP",
                    Severity = "Error",
                    API = "GetProductionSecrets"
                });

                return false;
            }

            return true;
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
                Console.WriteLine("Didn't create DB tables, this is expected if they already exist");
            }

            return false;
        }

        public static async Task<byte[]> GetEntropyBytes()
        {
            if (!File.Exists(UniqueEntropyBytesPath))
            {
                await File.WriteAllBytesAsync(UniqueEntropyBytesPath, UTF8Encoding.UTF8.GetBytes(Utilities.RandomByteString()));
            }

            ApiEnvironment.EntropyBytes = await File.ReadAllBytesAsync(UniqueEntropyBytesPath);

            return ApiEnvironment.EntropyBytes;
        }

        // Gets the DeviceIdTokenSeed used as static entropy in DeviceId generation
        private static async Task<string> GetDeviceIdTokenSeed()
        {
            try
            {
                if (!File.Exists(DeviceTokenPath))
                {
                    await File.WriteAllTextAsync(DeviceTokenPath, Guid.NewGuid().ToString());
                }

                return await File.ReadAllTextAsync(DeviceTokenPath);
            }
            catch(Exception ex)
            {
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = $"Exception GetDeviceIdToken: {ex.Message} Inner Exception: {ex.InnerException.Message ?? string.Empty}",
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

