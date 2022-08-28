using System.Dynamic;
using System.Text.Json;

namespace FFPP.Common
{
    public class Sam
    {
        public enum SamAppType { Api, Spa };

        public static async Task<SamAndPassword> CreateSAMAuthApp(string appName, SamAppType appType, string domain, string swaggerUiAppId="", string[]? spaRedirectUri= null, string scopeGuid="")
        {
            dynamic samApp;

            switch (appType)
            {
                case SamAppType.Api:
                    samApp = new ExpandoObject();
                    samApp.displayName = appName;
                    samApp.requiredResourceAccess = new List<RequiredResourceAccess>()
                    {
                        new RequiredResourceAccess()
                        {
                            resourceAccess = new()
                            {
								new()
								{
									id = new Guid("128ca929-1a19-45e6-a3b8-435ec44a36ba"),
									type = "Scope"
								},
								new()
                                {
                                    id = new Guid("e1fe6dd8-ba31-4d61-89e7-88639da4683d"),
                                    type = "Scope"
                                },
								new()
								{
									id = new Guid("aa07f155-3612-49b8-a147-6c590df35536"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("0f4595f7-64b1-4e13-81bc-11a249df07a9"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("73e75199-7c3e-41bb-9357-167164dbb415"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("7ab1d787-bae7-4d5d-8db6-37ea32df9186"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("d01b97e9-cbc0-49fe-810a-750afd5527a3"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("46ca0847-7e6b-426e-9775-ea810a948356"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("dc38509c-b87d-4da0-bd92-6bec988bac4a"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("7427e0e9-2fba-42fe-b0c0-848c9e6a8182"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("ad902697-1014-4ef5-81ef-2b4301988e8c"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("572fea84-0151-49b2-9301-11cb16974376"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("e4c9e354-4dc5-45b8-9e7c-e1393b0b1a20"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("0883f392-0a7a-443d-8c76-16a6d39c7b63"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("7b3f05d5-f68c-4b8d-8c59-a2ecd12f24af"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("0c5e8a55-87a6-4556-93ab-adc52c4d862d"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("44642bfe-8385-4adc-8fc6-fe3cb2c375c3"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("662ed50a-ac44-4eef-ad86-62eed9be2a29"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("8696daa5-bce5-4b2e-83f9-51b6defc4e1e"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("6aedf524-7e1c-45a7-bd76-ded8cab8d0fc"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("bac3b9c2-b516-4ef4-bd3b-c2ef73d8d804"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("11d4cd79-5ba5-460f-803f-e22c8ab85ccd"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("02e97553-ed7b-43d0-ab3c-f8bace0d040c"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("89fe6a52-be36-487e-b7d8-d061c450a026"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("a367ab51-6b49-43bf-a716-a1fb06d2a174"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("204e0828-b5ca-4ad8-b9f3-f32a958e7cc4"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("4e46008b-f24c-477d-8fff-7bb4ec7aafe0"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("0e263e50-5827-48a4-b97c-d940288653c7"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("e383f46e-2787-4529-855e-0e479a3ffac0"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("37f7f235-527c-4136-accd-4a02d197296e"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("14dad69e-099b-42c9-810b-d002981feec1"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("f6a3db3e-f7e8-4ed2-a414-557c8c9830be"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("0e755559-83fb-4b44-91d0-4cc721b9323e"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("a84a9652-ffd3-496e-a991-22ba5529156a"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("1d89d70c-dcac-4248-b214-903c457af83a"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("2b61aa8a-6d36-4b2f-ac7b-f29867937c53"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("ebf0f66e-9fb1-49e4-a278-222f76911cf4"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("c79f8feb-a9db-4090-85f9-90d820caa0eb"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("bdfbf15f-ee85-4955-8675-146e8e5296b5"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("f81125ac-d3b7-4573-a3b2-7099cc39df9e"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("cac97e40-6730-457d-ad8d-4852fddab7ad"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("b7887744-6746-4312-813d-72daeaee7e2d"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("48971fc1-70d7-4245-af77-0beb29b53ee2"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("aec28ec7-4d02-4e8c-b864-50163aea77eb"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("a9ff19c2-f369-4a95-9a25-ba9d460efc8e"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("59dacb05-e88d-4c13-a684-59f1afc8cc98"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("b98bfd41-87c6-45cc-b104-e2de4f0dafb9"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("2f9ee017-59c1-4f1d-9472-bd5529a7b311"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("951183d1-1a61-466f-a6d1-1fde911bfd95"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("637d7bec-b31e-4deb-acc9-24275642a2c9"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("101147cf-4178-4455-9d58-02b5c164e759"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("cc83893a-e232-4723-b5af-bd0b01bcfe65"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("233e0cf1-dd62-48bc-b65b-b38fe87fcf8e"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("d649fb7c-72b4-4eec-b2b4-b15acf79e378"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("485be79e-c497-4b35-9400-0e3fa7f2a5d4"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("9d8982ae-4365-4f57-95e9-d6032a4c0b87"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("48638b3c-ad68-4383-8ac4-e6880ee6ca57"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("39d65650-9d3e-4223-80db-a335590d027e"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("4a06efd2-f825-4e34-813e-82a57b03d1ee"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("f3bfad56-966e-4590-a536-82ecf548ac1e"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("4d135e65-66b8-41a8-9f8b-081452c91774"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("2eadaff8-0bce-4198-a6b9-2cfc35a30075"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("0c3e411a-ce45-4cd1-8f30-f99a3efa7b11"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("edb72de9-4252-4d03-a925-451deef99db7"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("767156cb-16ae-4d10-8f8b-41b657c8c8c8"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("7e823077-d88e-468f-a337-e18f1f0e6c7c"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("edd3c878-b384-41fd-95ad-e7407dd775be"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("40b534c3-9552-4550-901b-23879c90bcf9"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("bf3fbf03-f35f-4e93-963e-47e4d874c37a"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("5248dcb1-f83b-4ec3-9f4d-a4428a961a72"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("c395395c-ff9a-4dba-bc1f-8372ba9dca84"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("2e25a044-2580-450d-8859-42eeb6e996c0"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("0ce33576-30e8-43b7-99e5-62f8569a4002"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("207e0cb1-3ce7-4922-b991-5a760c346ebc"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("093f8818-d05f-49b8-95bc-9d2a73e9a43c"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("7825d5d6-6049-4ce7-bdf6-3b8d53f4bcd0"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("2104a4db-3a2f-4ea0-9dba-143d457dc666"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("eda39fa6-f8cf-4c3c-a909-432c683e4c9b"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("55896846-df78-47a7-aa94-8d3d4442ca7f"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("aa85bf13-d771-4d5d-a9e6-bca04ce44edf"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("ee928332-e9c2-4747-b4a0-f8c164b68de6"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("c975dd04-a06e-4fbb-9704-62daad77bb49"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("c37c9b61-7762-4bff-a156-afc0005847a0"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("34bf0e97-1971-4929-b999-9e2442d941d7"),
									type = "Role"
								},
								new()
								{
									id = new Guid("19b94e34-907c-4f43-bde9-38b1909ed408"),
									type = "Role"
								},
								new()
								{
									id = new Guid("999f8c63-0a38-4f1b-91fd-ed1947bdd1a9"),
									type = "Role"
								},
								new()
								{
									id = new Guid("292d869f-3427-49a8-9dab-8c70152b74e9"),
									type = "Role"
								},
								new()
								{
									id = new Guid("2f51be20-0bb4-4fed-bf7b-db946066c75e"),
									type = "Role"
								},
								new()
								{
									id = new Guid("58ca0d9a-1575-47e1-a3cb-007ef2e4583b"),
									type = "Role"
								},
								new()
								{
									id = new Guid("06a5fe6d-c49d-46a7-b082-56b1b14103c7"),
									type = "Role"
								},
								new()
								{
									id = new Guid("246dd0d5-5bd0-4def-940b-0421030a5b68"),
									type = "Role"
								},
								new()
								{
									id = new Guid("bf394140-e372-4bf9-a898-299cfc7564e5"),
									type = "Role"
								},
								new()
								{
									id = new Guid("741f803b-c850-494e-b5df-cde7c675a1ca"),
									type = "Role"
								},
								new()
								{
									id = new Guid("230c1aed-a721-4c5d-9cb4-a90514e508ef"),
									type = "Role"
								},
								new()
								{
									id = new Guid("b633e1c5-b582-4048-a93e-9f11b44c7e96"),
									type = "Role"
								},
								new()
								{
									id = new Guid("5b567255-7703-4780-807c-7be8301ae99b"),
									type = "Role"
								},
								new()
								{
									id = new Guid("62a82d76-70ea-41e2-9197-370581804d09"),
									type = "Role"
								},
								new()
								{
									id = new Guid("7ab1d382-f21e-4acd-a863-ba3e13f7da61"),
									type = "Role"
								},
								new()
								{
									id = new Guid("1138cb37-bd11-4084-a2b7-9f71582aeddb"),
									type = "Role"
								},
								new()
								{
									id = new Guid("78145de6-330d-4800-a6ce-494ff2d33d07"),
									type = "Role"
								},
								new()
								{
									id = new Guid("9241abd9-d0e6-425a-bd4f-47ba86e767a4"),
									type = "Role"
								},
								new()
								{
									id = new Guid("5b07b0dd-2377-4e44-a38d-703f09a0dc3c"),
									type = "Role"
								},
								new()
								{
									id = new Guid("243333ab-4d21-40cb-a475-36241daa0842"),
									type = "Role"
								},
								new()
								{
									id = new Guid("e330c4f0-4170-414e-a55a-2f022ec2b57b"),
									type = "Role"
								},
								new()
								{
									id = new Guid("5ac13192-7ace-4fcf-b828-1a26f28068ee"),
									type = "Role"
								},
								new()
								{
									id = new Guid("2f6817f8-7b12-4f0f-bc18-eeaf60705a9e"),
									type = "Role"
								},
								new()
								{
									id = new Guid("dbaae8cf-10b5-4b86-a4a1-f871c94c6695"),
									type = "Role"
								},
								new()
								{
									id = new Guid("bf7b1a76-6e77-406b-b258-bf5c7720e98f"),
									type = "Role"
								},
								new()
								{
									id = new Guid("01c0a623-fc9b-48e9-b794-0756f8e8f067"),
									type = "Role"
								},
								new()
								{
									id = new Guid("50483e42-d915-4231-9639-7fdb7fd190e5"),
									type = "Role"
								},
								new()
								{
									id = new Guid("dbb9058a-0e50-45d7-ae91-66909b5d4664"),
									type = "Role"
								},
								new()
								{
									id = new Guid("a82116e5-55eb-4c41-a434-62fe8a61c773"),
									type = "Role"
								},
								new()
								{
									id = new Guid("f3a65bd4-b703-46df-8f7e-0174fea562aa"),
									type = "Role"
								},
								new()
								{
									id = new Guid("59a6b24b-4225-4393-8165-ebaec5f55d7a"),
									type = "Role"
								},
								new()
								{
									id = new Guid("0121dc95-1b9f-4aed-8bac-58c5ac466691"),
									type = "Role"
								},
								new()
								{
									id = new Guid("3b55498e-47ec-484f-8136-9013221c06a9"),
									type = "Role"
								},
								new()
								{
									id = new Guid("35930dcf-aceb-4bd1-b99a-8ffed403c974"),
									type = "Role"
								},
								new()
								{
									id = new Guid("25f85f3c-f66c-4205-8cd5-de92dd7f0cec"),
									type = "Role"
								},
								new()
								{
									id = new Guid("29c18626-4985-4dcd-85c0-193eef327366"),
									type = "Role"
								},
								new()
								{
									id = new Guid("4437522e-9a86-4a41-a7da-e380edd4a97d"),
									type = "Role"
								}
							},
                            resourceAppId = new Guid("0000000300000000c000000000000000")
                        },
                        new RequiredResourceAccess()
                        {
                            resourceAccess = new()
                            {
                                new()
                                {
                                    id = new Guid("1cebfa2a-fb4d-419e-b5f9-839b4383e05a"),
                                    type = "Scope"
                                }
                            },
                            resourceAppId = new Guid("fa3d9a0c-3fb0-42cc-9193-47c7ecd2edbd")
                        },
						new RequiredResourceAccess()
						{
							resourceAccess = new()
							{
								new()
								{
									id = new Guid("5778995a-e1bf-45b8-affa-663a9f3f4d04"),
									type = "Role"
								},
								new()
								{
									id = new Guid("a42657d6-7f20-40e3-b6f0-cee03008a62a"),
									type = "Scope"
								},
								new()
								{
									id = new Guid("311a71cc-e848-46a1-bdf8-97ff7156d8e6"),
									type = "Scope"
								},
							},
							resourceAppId = new Guid("00000002-0000-0000-c000-000000000000")
						}

					};

                    if (!swaggerUiAppId.Equals(string.Empty) && !scopeGuid.Equals(string.Empty))
                    {
                        samApp.identifierUris = new List<string>() { string.Format("https://{0}/{1}", domain, Guid.NewGuid().ToString()) };

                        samApp.api = new ApiApplication()
                        {
                            acceptMappedClaims = null,
                            knownClientApplications = new List<string>(){},
                            requestedAccessTokenVersion = 2,
                            oauth2PermissionScopes = new List<PermissionScope>()
                            {
                                new PermissionScope
                                {
                                    id = scopeGuid,
                                    adminConsentDescription = "access the api",
                                    adminConsentDisplayName = "access the api",
                                    isEnabled = true,
                                    type = "Admin",
                                    userConsentDescription = "access the api",
                                    userConsentDisplayName = "access the api",
                                    value = ApiEnvironment.ApiAccessScope
                                }
                            },
                            preAuthorizedApplications = new()
                            {
                                new PreAuthorizedApplication
                                {
                                    appId = swaggerUiAppId,
                                    delegatedPermissionIds = new() { scopeGuid.ToString() }
                                }
                            }
                        };
                    }
                    else
                    {
                        samApp.identifierUris = new List<string>() { string.Format("https://{0}/{1}", domain, Guid.NewGuid().ToString()) };

						samApp.api = new ApiApplication()
						{
							acceptMappedClaims = null,
							knownClientApplications = new List<string>()
							{
							},
							requestedAccessTokenVersion = 2,
							oauth2PermissionScopes = new List<PermissionScope>()
						{

							new PermissionScope
							{
								id = Guid.NewGuid().ToString(),
								adminConsentDescription = "access the api",
								adminConsentDisplayName = "access the api",
								isEnabled = true,
								type = "Admin",
								userConsentDescription = "access the api",
								userConsentDisplayName = "access the api",
								value = ApiEnvironment.ApiAccessScope
                            }
                        },
                            preAuthorizedApplications = new() { }
                        };
                    }
                    samApp.appRoles = new List<AppRole>()
                    {
                        new()
                        {
                            allowedMemberTypes = new() { "User" },
                            description = "reader",
                            displayName = "reader",
                            id = Guid.NewGuid().ToString(),
                            isEnabled = true,
                            origin = "application",
                            value = "reader"
                        },
                        new()
                        {
                            allowedMemberTypes = new() { "User" },
                            description = "editor",
                            displayName = "editor",
                            id = Guid.NewGuid().ToString(),
                            isEnabled = true,
                            origin = "application",
                            value = "editor"
                        },
                        new()
                        {
                            allowedMemberTypes = new() { "User" },
                            description = "admin",
                            displayName = "admin",
                            id = Guid.NewGuid().ToString(),
                            isEnabled = true,
                            origin = "application",
                            value = "admin"
                        },
                        new()
                        {
                            allowedMemberTypes = new() { "User" },
                            description = "owner",
                            displayName = "owner",
                            id = Guid.NewGuid().ToString(),
                            isEnabled = true,
                            origin = "application",
                            value = "owner"
                        }
                    };
                    samApp.signInAudience = "AzureADMultipleOrgs";
					samApp.isFallbackPublicClient = true;
					string ffppFrontEnd = ApiEnvironment.FfppFrontEndUri.TrimEnd('/');
					samApp.web = new Web()
					{
						redirectUris = new()
						{
                            $"{ApiEnvironment.KestrelHttp}",
                            $"{ApiEnvironment.KestrelHttps}",
                            ffppFrontEnd,
                            $"{ffppFrontEnd}/bootstrap/receivegraphtoken",
                            $"{ApiEnvironment.KestrelHttp}/bootstrap/receivegraphtoken",
                            $"{ApiEnvironment.KestrelHttps}/bootstrap/receivegraphtoken",
                            "https://login.microsoftonline.com/common/oauth2/nativeclient",
                            "urn:ietf:wg:oauth:2.0:oob"
                        },
						implicitGrantSettings = new()
						{
							enableAccessTokenIssuance = true,
							enableIdTokenIssuance = true
                        }
					};

					JsonElement createdSamApp = await RequestHelper.NewGraphPostRequest("https://graph.microsoft.com/v1.0/applications", ApiEnvironment.Secrets.TenantId, samApp, HttpMethod.Post, "https://graph.microsoft.com/.default", true);
                    Console.WriteLine("Waiting 30 seconds for app to progagate through Azure before setting a password on it...");
                    await Task.Delay(30000); // Have to wait about 30 seconds for Azure to properly replicate the app before we can set password on it
					var appPasswordJson = await RequestHelper.NewGraphPostRequest(string.Format("https://graph.microsoft.com/v1.0/applications/{0}/addPassword", createdSamApp.GetProperty("id").GetString()), ApiEnvironment.Secrets.TenantId, new PasswordCredential() { displayName="FFPP-Pwd" }, HttpMethod.Post, "https://graph.microsoft.com/.default", true);
					return new() { sam=createdSamApp, appPassword=appPasswordJson.GetProperty("secretText").GetString() ?? string.Empty };

				case SamAppType.Spa:
                    samApp = new ExpandoObject();
                    samApp.displayName = appName;
                    samApp.signInAudience = "AzureADMyOrg";
                    samApp.requiredResourceAccess = new List<RequiredResourceAccess>() { new RequiredResourceAccess(){ resourceAccess = new List<ResourceAccess>() { new() { id = new Guid("e1fe6dd8ba314d6189e788639da4683d"), type = "Scope" } }, resourceAppId = new Guid("0000000300000000c000000000000000") } };

                    if (spaRedirectUri != null)
                    {
                        samApp.spa = new Spa() { redirectUris = spaRedirectUri };
                    }

                    return new() { sam = await RequestHelper.NewGraphPostRequest("https://graph.microsoft.com/v1.0/applications", ApiEnvironment.Secrets.TenantId, samApp, HttpMethod.Post, "https://graph.microsoft.com/.default", true)};
            }

            return new();
        }

		public struct SamAndPassword
        {
			public JsonElement sam { get; set; }
			public string? appPassword { get; set; }
        }

        public struct ResourceAccess
        {
            public Guid id { get; set; }
            public string type { get; set; }
        }

        public struct RequiredResourceAccess
        {
            public List<ResourceAccess> resourceAccess { get; set; }
            public Guid resourceAppId { get; set; }
        }

        public struct Spa
        {
            public string[]? redirectUris { get; set; }
        }

        public struct ApiApplication
        {
            public bool? acceptMappedClaims { get; set; }
            public List<string>? knownClientApplications { get; set; }
            public List<PermissionScope>? oauth2PermissionScopes { get; set; }
            public List<PreAuthorizedApplication>? preAuthorizedApplications { get; set; }
            public int? requestedAccessTokenVersion { get; set; }
        }

        public struct PermissionScope
        {
            public string? id { get; set; }
            public string? adminConsentDisplayName { get; set; }
            public string? adminConsentDescription { get; set; }
            public string? userConsentDisplayName { get; set; }
            public string? userConsentDescription { get; set; }
            public string? value { get; set; }
            public string? type { get; set; }
            public bool? isEnabled { get; set; }
        }

        public struct PreAuthorizedApplication
        {
            public string? appId { get; set; }
            public List<string>? delegatedPermissionIds { get; set; }
        }


        public struct AppRole
        {
            public List<string>? allowedMemberTypes { get; set; }
            public string? description { get; set; }
            public string? displayName { get; set; }
            public string? id { get; set; }
            public bool? isEnabled { get; set; }
            public string? origin { get; set; }
            public string? value { get; set; }
        }

		public struct Web
        {
			public List<string> redirectUris { get; set; }
			public ImplicitGrantSettings implicitGrantSettings { get; set; }
		}

		public struct ImplicitGrantSettings
		{
			public bool enableAccessTokenIssuance { get; set; }
			public bool enableIdTokenIssuance { get; set; }
		}

		public struct PasswordCredential
		{
			public string displayName { get; set; }
		}
    }
}

