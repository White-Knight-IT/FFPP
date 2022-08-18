using System.Text.Json;
using FFPP.Common;
using FFPP.Data;
using FFPP.Data.Logging;

namespace FFPP.Api.v10.Tenants
{
    public class Tenant
    {
		public string? customerId { get; set; }
		public string? defaultDomainName { get; set; }
		public string? displayName { get; set; }

		/// <summary>
        /// Gets a CustomerId from a known DefaultDomain
        /// </summary>
        /// <param name="defaultDomain">DefaultDomain that we wish to find the matching CustomerId</param>
        /// <returns>ClientId</returns>
		public async static Task<string> GetCustomerIdFromDefaultDomain(string defaultDomain)
        {
			return (await GetTenants(string.Empty, false)).Find(x => x.defaultDomainName.Equals(defaultDomain)).customerId ?? string.Empty;
        }

		/// <summary>
		/// Gets a DefaultDomain from a known CustomerId
		/// </summary>
		/// <param name="customerId">CustomerId that we wish to find the matching DefaultDomain</param>
		/// <returns></returns>
		public async static Task<string> GetDefaultDomainFromCustomerId(string customerId)
		{
			return (await GetTenants(string.Empty, false)).Find(x => x.customerId.Equals(customerId)).defaultDomainName ?? string.Empty;
		}
		/// <summary>
		/// Returns the tenants managed in a partner relationship
		/// </summary>
		/// <param name="exclude">True excludes ExcludedTenants</param>
		/// <returns>Tenants managed in a partner relationship</returns>
		public async static Task<List<Tenant>> GetTenants(string accessingUser, bool exclude = true, bool allTenantSelector = false)
		{	
            FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
            {
                Message = "Accessed this API",
                Username = accessingUser,
                Severity = "Debug",
                API = "ListTenants"
            });

            List<Tenant> allTenants = new();
			List<Tenant> outTenants = new();

			FileInfo cacheFile = new(ApiEnvironment.CachedTenantsFile);
			string uri = "https://graph.microsoft.com/beta/contracts?$select=customerId,defaultDomainName,displayName&$top=999";
			
			void CheckExclusions(List<Tenant> tenantArray, ref List<Tenant> listTenants, ref List<Tenant> allTenants)
            {
				if(allTenantSelector)
                {
                    listTenants.Add(new Tenant()
					{
						customerId= "AllTenants",
						defaultDomainName = "AllTenants",
						displayName = "* All Tenants"
					});
                }

				foreach (Tenant t in tenantArray)
				{
					allTenants.Add(t);

					// If we want to exclude from ExcludedTenants from outTenants and it is in ExcludedTenants
					if (exclude && ExcludedTenantsDbThreadSafeCoordinator.ThreadSafeTenantIsExcluded(t.defaultDomainName).Result)
					{
						// We exclude
						continue;
					}

					listTenants.Add(t);
				}
            }

			if(cacheFile.Exists && cacheFile.LastWriteTimeUtc >= DateTime.UtcNow.AddMinutes(-7))
            {
				//Read tenants from cache as they were cached in last 15m
				CheckExclusions(await Utilities.ReadJsonFromFile<List<Tenant>>(cacheFile.FullName),ref outTenants, ref allTenants);
				return outTenants;

			}

			List<JsonElement> tenants = await RequestHelper.NewGraphGetRequest(uri, ApiEnvironment.Secrets.TenantId);

			List<Tenant> tenantArrayList = Utilities.ParseJson<Tenant>(tenants);

			CheckExclusions(tenantArrayList, ref outTenants, ref allTenants);

			// We write all tenants to cache not just unexcluded tenants
			Utilities.WriteJsonToFile<List<Tenant>>(allTenants, cacheFile.FullName);
			return outTenants;
		}
	}
}

