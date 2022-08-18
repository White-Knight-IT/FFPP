using FFPP.Data.Logging;
using FFPP.Api.v10.Tenants;
using FFPP.Common;

namespace FFPP.Api.v10.Dashboards
{
    public class FfppDashboard
    {
        public string? NextStandardsRun { get; set; }
        public string? NextBPARun { get; set; }
        public string? NextDomainsRun { get; set; }
        public int? queuedApps { get; set; }
        public int? queuedStandards { get; set; }
        public int? tenantCount { get; set; }
        public string? RefreshTokenDate { get; set; }
        public string? ExchangeTokenDate { get; set; }
        public List<DashLogEntry>? LastLog { get; set; }
        public List<string>? Alerts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async static Task<FfppDashboard> GetHomeData(string accessingUser)
        {
            List<DashLogEntry> last10Logs = new();

            foreach (FfppLogsDbContext.LogEntry log in await FfppLogsDbThreadSafeCoordinator.ThreadSafeTop10Logs())
            {
                last10Logs.Add(new DashLogEntry() { Tenant = log.Tenant ?? string.Empty, Message = log.Message ?? string.Empty });
            }

            FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
            {
                Message = "Accessed this API",
                Username = accessingUser,
                Severity = "Debug",
                API = "GetDashboard"
            });

            return new FfppDashboard()
            {
                NextStandardsRun = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-ddThh:mm:ss"),
                NextBPARun = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-ddThh:mm:ss"),
                queuedApps = 0,
                queuedStandards = 0,
                tenantCount = (await Tenant.GetTenants(string.Empty, allTenantSelector: false)).Count,
                RefreshTokenDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                ExchangeTokenDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                LastLog = last10Logs,
                Alerts = AlertChecks()
            };
        }

        /// <summary>
        /// Holds Tenant:Message values of a log entry for the Dashboard to consume
        /// </summary>
        public struct DashLogEntry
        {
            public string Tenant { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ffppVersion"></param>
        /// <returns></returns>
        public static async Task<Versions> CheckVersions(string accessingUser, string ffppVersion)
        {
            FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
            {
                Message = "Accessed this API",
                Username = accessingUser,
                Severity = "Debug",
                API = "GetVersion"
            });

            HttpRequestMessage requestMessage = new(HttpMethod.Get, ApiEnvironment.RemoteFfppVersion);
            HttpResponseMessage responseMessage = await RequestHelper.SendHttpRequest(requestMessage);

            Version remoteFfppVersion = Version.Parse((await responseMessage.Content.ReadAsStringAsync()).Split(":")[0]);

            return new Versions()
            {
                LocalFFPPVersion = ffppVersion,
                RemoteFFPPVersion = remoteFfppVersion.ToString(),
                OutOfDateFFPP = remoteFfppVersion > Version.Parse(ffppVersion),
            };
        }

        public struct Versions
        {
            public string LocalFFPPVersion { get; set; }
            public string RemoteFFPPVersion { get; set; }
            public bool OutOfDateFFPP { get; set; }

        }

        #region Private Methods
        // Checks for alert conditions then builds alerts if the conditions are satisfied
        private static List<string> AlertChecks()
        {
            List<string> alerts = new();

            // ApplicationId does not exist - setup SAM (Azure AD Enterprise Application)
            if (ApiEnvironment.Secrets.ApplicationId == null || ApiEnvironment.Secrets.ApplicationId.ToLower().Equals("longapplicationid"))
            {
                alerts.Add("You have not yet setup your SAM (Azure AD Enterprise Application). Please go to the SAM Wizard in settings to finish setup.");
            }

            // Fake alerts, uncomment when you want to test

            alerts.Add("I can't beleive it's not butter!");
            alerts.Add("I really can't beleive it's not butter!");
            alerts.Add("I still can't beleive it's not butter!");
            alerts.Add("Ok, it's not butter.");
            alerts.Add("It is a fact that Kelvin hates camels.");

            return alerts;
        }
        #endregion
    }
}

