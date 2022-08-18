
using System.Text.Json;
using FFPP.Common;
using FFPP.Data.Logging;

namespace FFPP.Api.v10.Tenants
{
    public class Domain
    {
		public string? authenticationType { get; set; }
		public string? availabilityStatus { get; set; }
		public string? id { get; set; }
		public bool? isAdminManaged { get; set; }
		public bool? isDefault { get; set; }
		public bool? isInitial { get; set; }
		public bool? isRoot { get; set; }
		public bool? isVerified { get; set; }
		public int? passwordNotificationWindowInDays { get; set; }
		public int? passwordValidityPeriodInDays { get; set; }
		public DomainState? state { get; set; }
		public List<string>? supportedServices { get; set; }

		/// <summary>
        /// 
        /// </summary>
        /// <param name="tenantFilter"></param>
        /// <returns></returns>
		public async static Task<List<Domain>> GetDomains(string accessingUser, string tenantFilter)
		{
			List<Domain> outDomains = new();

            FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
            {
                Message = "Accessed this API",
                Username = accessingUser,
                Severity = "Debug",
                API = "ListDomains",
				Tenant = tenantFilter
            });

            List<JsonElement> domainsRaw = await RequestHelper.NewGraphGetRequest("https://graph.microsoft.com/beta/domains", tenantFilter);
			List<Domain> domainsArrayList = Utilities.ParseJson<Domain>(domainsRaw);

			foreach (Domain dom in domainsArrayList)
			{
				outDomains.Add(dom);
			}
			
			return outDomains.OrderBy(x => x.isDefault).ToList();
		}

		public struct DomainState
		{
			public string? lastActionDateTime { get; set; }
			public string? operation { get; set; }
			public string? status { get; set; }
		}
	}
}

