using System.Net.Http.Headers;
using System.Xml;
using System.Xml.Serialization;
using FFPP.Api.v10.Tenants;
using FFPP.Common;

namespace FFPP.Api.v10.Users
{
    [XmlRoot("User", Namespace = "http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration")]
    public class MsolUser
    {
        /// <summary>
        /// Gets a list of all the users in the tenant
        /// </summary>
        /// <param name="tenant">Tenant default domain name</param>
        /// <returns>All users in the supplied tenant default domain name</returns>
        public static async Task<List<MsolUser>> GetCippMsolUsers(string tenant)
        {
            List<MsolUser> users = new();
            Dictionary<string, string> aadGraphToken = await RequestHelper.GetGraphToken("", false, scope: "https://graph.windows.net/.default");
            string tenantId = tenant;
            // Get CustomnerId if tenantId is a domain
            if (tenantId.Contains('.'))
            {
                tenantId = await Tenant.GetCustomerIdFromDefaultDomain(tenant);
            }
            string trackingGuid = Guid.NewGuid().ToString();
            string logonPost = string.Format("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\">" +
                "<s:Header><a:Action s:mustUnderstand=\"1\">http://provisioning.microsoftonline.com/IProvisioningWebService/MsolConnect</a:Action><a:MessageID>" +
                "urn:uuid:{0}</a:MessageID><a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>" +
                "<UserIdentityHeader xmlns=\"http://provisioning.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<BearerToken xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">{1}</BearerToken>" +
                "<LiveToken i:nil=\"true\" xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\"/></UserIdentityHeader>" +
                "<ClientVersionHeader xmlns=\"http://provisioning.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<ClientId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">50afce61-c917-435b-8c6d-60aa5a8b8aa7</ClientId>" +
                "<Version xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">1.2.183.57</Version></ClientVersionHeader>" +
                "<ContractVersionHeader xmlns=\"http://becwebservice.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<BecVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">Version47</BecVersion></ContractVersionHeader>" +
                "<TrackingHeader xmlns=\"http://becwebservice.microsoftonline.com/\">{0}</TrackingHeader><a:To s:mustUnderstand=\"1\">" +
                "https://provisioningapi.microsoftonline.com/provisioningwebservice.svc</a:To></s:Header><s:Body><MsolConnect xmlns=\"http://provisioning.microsoftonline.com/\">" +
                "<request xmlns:b=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<b:BecVersion>Version4</b:BecVersion><b:TenantId i:nil=\"true\"/><b:VerifiedDomain i:nil=\"true\"/></request></MsolConnect></s:Body></s:Envelope>",
                trackingGuid, aadGraphToken.GetValueOrDefault("Authorization", "FAILED-TO-GET-AUTH-TOKEN"));

            string msolXml = string.Empty;
            string dataBlob = string.Empty;

            using (HttpRequestMessage requestMessage = new(HttpMethod.Post, "https://provisioningapi.microsoftonline.com/provisioningwebservice.svc"))
            {
                requestMessage.Content = new StringContent(logonPost);
                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml");

                HttpResponseMessage responseMessage = await RequestHelper.SendHttpRequest(requestMessage);

                XmlDocument xmlDoc = new();
                xmlDoc.Load(await responseMessage.Content.ReadAsStreamAsync());
                dataBlob = xmlDoc.GetElementsByTagName("DataBlob")[0].InnerText;

                msolXml = string.Format("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\"><s:Header>" +
                    "<a:Action s:mustUnderstand=\"1\">http://provisioning.microsoftonline.com/IProvisioningWebService/ListUsers</a:Action><a:MessageID>urn:uuid:{0}</a:MessageID>" +
                    "<a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>" +
                    "<UserIdentityHeader xmlns=\"http://provisioning.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    "<BearerToken xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">{1}</BearerToken>" +
                    "<LiveToken i:nil=\"true\" xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\"/></UserIdentityHeader>" +
                    "<BecContext xmlns=\"http://becwebservice.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    "<DataBlob xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">{2}</DataBlob>" +
                    "<PartitionId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">2</PartitionId></BecContext>" +
                    "<ClientVersionHeader xmlns=\"http://provisioning.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    "<ClientId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">50afce61-c917-435b-8c6d-60aa5a8b8aa7</ClientId>" +
                    "<Version xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">1.2.183.57</Version></ClientVersionHeader>" +
                    "<ContractVersionHeader xmlns=\"http://becwebservice.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    "<BecVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">Version47</BecVersion></ContractVersionHeader>" +
                    "<TrackingHeader xmlns=\"http://becwebservice.microsoftonline.com/\">4e6cb653-c968-4a3a-8a11-2c8919218aeb</TrackingHeader><a:To s:mustUnderstand=\"1\">" +
                    "https://provisioningapi.microsoftonline.com/provisioningwebservice.svc</a:To></s:Header><s:Body><ListUsers xmlns=\"http://provisioning.microsoftonline.com/\">" +
                    "<request xmlns:b=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    "<b:BecVersion>Version16</b:BecVersion><b:TenantId>{3}</b:TenantId><b:VerifiedDomain i:nil=\"true\"/>" +
                    "<b:UserSearchDefinition xmlns:c=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration\"><c:PageSize>500</c:PageSize><c:SearchString i:nil=\"true\"/>" +
                    "<c:SortDirection>Ascending</c:SortDirection><c:SortField>None</c:SortField><c:AccountSku i:nil=\"true\"/><c:BlackberryUsersOnly i:nil=\"true\"/><c:City i:nil=\"true\"/>" +
                    "<c:Country i:nil=\"true\"/><c:Department i:nil=\"true\"/><c:DomainName i:nil=\"true\"/><c:EnabledFilter i:nil=\"true\"/><c:HasErrorsOnly i:nil=\"true\"/>" +
                    "<c:IncludedProperties i:nil=\"true\" xmlns:d=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><c:IndirectLicenseFilter i:nil=\"true\"/>" +
                    "<c:LicenseReconciliationNeededOnly i:nil=\"true\"/><c:ReturnDeletedUsers i:nil=\"true\"/><c:State i:nil=\"true\"/><c:Synchronized i:nil=\"true\"/><c:Title i:nil=\"true\"/>" +
                    "<c:UnlicensedUsersOnly i:nil=\"true\"/><c:UsageLocation i:nil=\"true\"/></b:UserSearchDefinition></request></ListUsers></s:Body></s:Envelope>",
                    trackingGuid, aadGraphToken.GetValueOrDefault("Authorization", "FAILED-TO-GET-AUTH-TOKEN"), dataBlob, tenantId);
            }

            using (HttpRequestMessage requestMessage = new(HttpMethod.Post, "https://provisioningapi.microsoftonline.com/provisioningwebservice.svc"))
            {
                bool lastPage = true;

                do
                {
                    requestMessage.Content = new StringContent(msolXml);
                    requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml");
                    HttpResponseMessage responseMessage = await RequestHelper.SendHttpRequest(requestMessage);
                    XmlDocument xmlDoc = new();
                    xmlDoc.Load(await responseMessage.Content.ReadAsStreamAsync());
                    XmlSerializer reader = new(typeof(MsolUser));

                    foreach (XmlNode u in xmlDoc.GetElementsByTagName("c:User"))
                    {
                        MsolUser userToAdd = (MsolUser)reader.Deserialize(new MemoryStream(System.Text.Encoding.Unicode.GetBytes(u.OuterXml)));

                        if (userToAdd != null)
                        {
                            userToAdd.RawXml = u;
                            users.Add(userToAdd);
                        }
                    }

                    lastPage = bool.Parse(xmlDoc.GetElementsByTagName("c:IsLastPage")[0].InnerText);
                    string listContext = xmlDoc.GetElementsByTagName("c:ListContext")[0].InnerText;
                    msolXml = string.Format("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\">" +
                        "<s:Header><a:Action s:mustUnderstand=\"1\">http://provisioning.microsoftonline.com/IProvisioningWebService/NavigateUserResults</a:Action>" +
                        "<a:MessageID>urn:uuid:{0}</a:MessageID><a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>" +
                        "<UserIdentityHeader xmlns=\"http://provisioning.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                        "<BearerToken xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">{1}</BearerToken>" +
                        "<LiveToken i:nil=\"true\" xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\"/></UserIdentityHeader>" +
                        "<BecContext xmlns=\"http://becwebservice.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                        "<DataBlob xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">{2}</DataBlob>" +
                        "<PartitionId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">130</PartitionId></BecContext>" +
                        "<ClientVersionHeader xmlns=\"http://provisioning.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                        "<ClientId xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">50afce61-c917-435b-8c6d-60aa5a8b8aa7</ClientId>" +
                        "<Version xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">1.2.183.57</Version></ClientVersionHeader>" +
                        "<ContractVersionHeader xmlns=\"http://becwebservice.microsoftonline.com/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                        "<BecVersion xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\">Version47</BecVersion>" +
                        "</ContractVersionHeader><TrackingHeader xmlns=\"http://becwebservice.microsoftonline.com/\">{0}</TrackingHeader><a:To s:mustUnderstand=\"1\">" +
                        "https://provisioningapi.microsoftonline.com/provisioningwebservice.svc</a:To></s:Header><s:Body>" +
                        "<NavigateUserResults xmlns=\"http://provisioning.microsoftonline.com/\"><" +
                        "request xmlns:b=\"http://schemas.datacontract.org/2004/07/Microsoft.Online.Administration.WebService\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                        "<b:BecVersion>Version16</b:BecVersion><b:TenantId>{3}</b:TenantId><b:VerifiedDomain i:nil=\"true\"/><b:ListContext>{4}</b:ListContext>" +
                        "<b:PageToNavigate>Next</b:PageToNavigate></request></NavigateUserResults></s:Body></s:Envelope>",
                        trackingGuid, aadGraphToken.GetValueOrDefault("Authorization", "FAILED-TO-GET-AUTH-TOKEN"), dataBlob, tenantId, listContext);
                }
                while (!lastPage || users == null);
            }

            return users;
        }

        public object? AlternateEmailAddresses { get; set; }
        public object? AlternateMobilePhones { get; set; }
        public string? AlternativeSecurityIds { get; set; }
        public bool? BlockCredential { get; set; }
        public string? City { get; set; }
        public int? CloudExchangeRecipientDisplayType { get; set; }
        public string? Country { get; set; }
        public string? Department { get; set; }
        public bool? DirSyncEnabled { get; set; }
        public string? DirSyncProvisioningErrors { get; set; }
        public string? DisplayName { get; set; }
        public bool? Errors { get; set; }
        public string? Fax { get; set; }
        public string? FirstName { get; set; }
        public string? ImmutableId { get; set; }
        public string? IndirectLicenseErrors { get; set; }
        public bool? IsBlackberryUser { get; set; }
        public bool? IsLicensed { get; set; }
        public string? LastDirSyncTime { get; set; }
        public string? LastName { get; set; }
        public string? LastPasswordChangeTimestamp { get; set; }
        public LicenseAssignmentDetails? LicenseAssignmentDetails { get; set; }
        public bool? LicenseReconciliationNeeded { get; set; }
        public Licenses? Licenses { get; set; }
        public string? LiveId { get; set; }
        public string? MSExchRecipientTypeDetails { get; set; }
        public string? MSRtcSipDeploymentLocator { get; set; }
        public string? MSRtcSipPrimaryUserAddress { get; set; }
        public string? MobilePhone { get; set; }
        public string? OathTokenMetadata { get; set; }
        public string? ObjectId { get; set; }
        public string? Office { get; set; }
        public string? OverallProvisioningStatus { get; set; }
        public bool? PasswordNeverExpires { get; set; }
        public bool? PasswordResetNotRequiredDuringActivate { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PortalSettings { get; set; }
        public string? PostalCode { get; set; }
        public string? PreferredDataLocation { get; set; }
        public string? PreferredLanguage { get; set; }
        public object? ProxyAddresses { get; set; }
        public XmlNode? RawXml { get; set; }
        public string? ReleaseTrack { get; set; }
        public ServiceInformation? ServiceInformation { get; set; }
        public string? SignInName { get; set; }
        public string? SoftDeletionTimestamp { get; set; }
        public string? State { get; set; }
        public string? StreetAddress { get; set; }
        public string? StrongAuthenticationMethods { get; set; }
        public string? StrongAuthenticationPhoneAppDetails { get; set; }
        public string? StrongAuthenticationProofupTime { get; set; }
        public string? StrongAuthenticationRequirements { get; set; }
        public string? StrongAuthenticationUserDetails { get; set; }
        public bool? StrongPasswordRequired { get; set; }
        public string? StsRefreshTokensValidFrom { get; set; }
        public string? Title { get; set; }
        public string? UsageLocation { get; set; }
        public string? UserLandingPageIdentifierForO365Shell { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? UserThemeIdentifierForO365Shell { get; set; }
        public string? UserType { get; set; }
        public string? ValidationStatus { get; set; }
        public string? WhenCreated { get; set; }

    }



    public struct LicenseAssignmentDetails
    {
        public LicenseAssignmentDetail LicenseAssignmentDetail { get; set; }
    }

    public struct LicenseAssignmentDetail
    {
        public AccountSku AccountSku { get; set; }
        public Assignments Assignments { get; set; }

    }

    public struct AccountSku
    {
        public string? AccountName { get; set; }
        public string? SkuPartNumber { get; set; }
    }

    public struct Assignments
    {
        public LicenseAssignment LicenseAssignment { get; set; }
    }

    public struct LicenseAssignment
    {
        [XmlElement("DisabledServicePlans")]
        public object? DisabledServicePlans { get; set; }
        public string? Error { get; set; }
        public string? ReferencedObjectId { get; set; }
        public string? Status { get; set; }
    }

    public struct ServicePlan
    {
        public string? ServiceName { get; set; }
        public string? ServicePlanId { get; set; }
        public string? ServiceType { get; set; }
        public string? TargetClass { get; set; }
    }

    public struct ServiceStatus
    {
        public string? ProvisioningStatus { get; set; }
        public ServicePlan? ServicePlan { get; set; }
    }

    public struct Licenses
    {
        public UserLicense UserLicense { get; set; }
    }

    public struct UserLicense
    {
        public AccountSku AccountSku;
        public string? AccountSkuId { get; set; }
        [XmlElement("GroupsAssigningLicense", Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
        public object? GroupsAssigningLicense { get; set; }
        public ServiceStatus[]? ServiceStatus { get; set; }
    }

    public struct ServiceParameters
    {
        [XmlElement("ServiceParameter")]
        public ServiceParameter ServiceParameter { get; set; }
    }

    public struct ServiceParameter
    {
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "Value")]
        public string Value { get; set; }
    }

    public struct ServiceExtention
    {
        [XmlElement("ServiceParameters")]
        public ServiceParameters ServiceParameters { get; set; }
    }

    public struct XElement
    {
        [XmlElement("ServiceExtention", Namespace = "http://schemas.microsoft.com/online/serviceextensions/2009/08/ExtensibilitySchema.xsd")]
        public ServiceExtention ServiceExtention { get; set; }
    }

    public struct ServiceInformation
    {
        [XmlElement("ServiceInformation")]
        public InternalServiceInformation? InternalServiceInformation { get; set; }
    }

    public struct ServiceElements
    {
        [XmlElement("XElement")]
        public XElement XElement { get; set; }
    }

    public struct InternalServiceInformation
    {
        public ServiceElements? ServiceElements { get; set; }
        public string? ServiceInstance { get; set; }
    }
}

