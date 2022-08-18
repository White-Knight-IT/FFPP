using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FFPP.Common;
using Microsoft.Graph;
using FFPP.Api.v10.Tenants;
using FFPP.Api.v10.Users;

namespace FFPP.Data.Logging
{
    /// <summary>
    /// Entity Framework Class used to create and manage FFPP Logs in a DB
    /// </summary>
    public class FfppLogsDbContext : DbContext
    {
        private DbSet<LogEntry>? _logEntries { get; set; }

        public FfppLogsDbContext()
        {

        }

        public async Task<List<LogEntry>> ListLogs()
        {
            return await _logEntries.ToListAsync() ?? new();
        }

        public async Task<List<LogEntry>> Top10Logs()
        {
            return await _logEntries.OrderByDescending(x => x.Timestamp).Take(10).ToListAsync() ?? new();
        }

        /// <summary>
        /// Writes to the console only if we are running in debug
        /// </summary>
        /// <param name="content">Content to write to console</param>
        /// <returns>bool which indicates successful write to console</returns>
        public static bool DebugConsoleWrite(string content)
        {
            if (ApiEnvironment.IsDebug)
            {
                Console.WriteLine(content);
                return true;
            }

            return false;
        }

        public async Task<bool> AddLogEntry(LogEntry logEntry)
        {
            try
            {
                logEntry.Timestamp = DateTime.UtcNow;

                if (string.IsNullOrEmpty(logEntry.Username))
                {
                    logEntry.Username = "FFPP";
                }

                if (string.IsNullOrEmpty(logEntry.Tenant))
                {
                    logEntry.Tenant = "None";
                }

                if (string.IsNullOrEmpty(logEntry.API))
                {
                    logEntry.API = "None";
                }

                if (null == logEntry.SentAsAlert)
                {
                    logEntry.SentAsAlert = false;
                }

                if (logEntry.Severity.ToLower().Equals("debug") && !ApiEnvironment.IsDebug)
                {
                    Console.WriteLine("Not writing to log file - Debug mode is not enabled.");
                }

                // Write to console for debug environment
                DebugConsoleWrite($"[ {DateTime.UtcNow.ToString()} ] - {logEntry.Severity} - {logEntry.Message} - {logEntry.Tenant} - {logEntry.API} - {logEntry.Username} - {logEntry.SentAsAlert.ToString()}");

                Task<bool> task = new(() =>
                {
                    int repeatOnFail = 5;
                    int attempts = 1;

                    do
                    {
                        try
                        {
                            Add(logEntry);
                            SaveChanges();
                            attempts = repeatOnFail + 1;
                            return true;
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
                Console.WriteLine("Exception writing log entry in FfppLogs: {0}, Inner Exception: {1}", ex.Message, ex.InnerException.Message ?? string.Empty);
            }

            return false;
        }

        // Tells EF that we want to use MySQL
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string connectionString = $"server={ApiEnvironment.MysqlServer};database=ffpp;user={ApiEnvironment.MysqlUser};password={ApiEnvironment.MysqlPassword};port={ApiEnvironment.MysqlServerPort}";
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        // Represents a LogEntry object as it exists in the FfppLogs DB
        public class LogEntry
        {
            [Key] // Public key
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto Generate GUID for our PK
            public Guid RowKey { get; set; }
            public DateTime Timestamp { get; set; }
            public string? Severity { get; set; }
            public string? Message { get; set; }
            public string? API { get; set; }
            public bool? SentAsAlert { get; set; }
            public string? Tenant { get; set; }
            public string? Username { get; set; }
        }
    }

    /// <summary>
    /// A class for accessing the FfppLogsDbContext in a thread safe manner
    /// </summary>
    public static class FfppLogsDbThreadSafeCoordinator
    {
        private static bool _locked = false;

        public static async Task<List<FfppLogsDbContext.LogEntry>> ThreadSafeTop10Logs()
        {
            WaitForUnlock();

                _locked = true;

            Task<List<FfppLogsDbContext.LogEntry>> getTop10 = new(() =>
            {
                using (FfppLogsDbContext logEntries = new())
                {
                    return logEntries.Top10Logs().Result;
                }
            });

            return await ExecuteQuery<List<FfppLogsDbContext.LogEntry>>(getTop10);
        }

        /// <summary>
        /// Add a log entry to the FfppLogsDbContext in a thread safe manner
        /// </summary>
        /// <param name="log">Log to add to DB</param>
        /// <returns>bool indicating success</returns>
        public static async Task<bool> ThreadSafeAdd(FfppLogsDbContext.LogEntry log)
        {
            WaitForUnlock();

            // By setting lock we do not allow any other DbContexts to be created and other queries will queue
            // until this value returns false;
            _locked = true;

            Task<bool> addLog = new(() =>
            {
                using (FfppLogsDbContext logEntries = new())
                {
                    return logEntries.AddLogEntry(log).Result;
                }
            });

            return await ExecuteQuery<bool>(addLog);
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

