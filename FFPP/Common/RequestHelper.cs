using System.Net;
using System.Net.Http.Headers;
using System.Web;
using System.Text.Json;
using FFPP.Api.v10.Tenants;
using FFPP.Data.Logging;
using Microsoft.Graph;
using static System.Net.WebRequestMethods;

namespace FFPP.Common
{
	/// <summary>
	/// Static class used as the basis of all outgoing HTTP requests
	/// </summary>
	public static class RequestHelper
	{
		private static readonly HttpClient _httpClient = new();

		/// <summary>
		/// Converts a known exception message into something more relatable to the user
		/// </summary>
		/// <param name="message">Exception message to make friendly</param>
		/// <returns>Friendly exception message</returns>
		public static string GetNormalizedError(string message)
		{
			return message switch
			{
				string a when a.Contains("Request not applicable to target tenant.") => "Required license not available for this tenant",
				string b when b.Contains("Neither tenant is B2C or tenant doesn't have premium license") => "This feature requires a P1 license or higher",
				string c when c.Contains("Response status code does not indicate success: 400 (Bad Request).") => "Error 400 occured. There is an issue with the token configuration for this tenant. Please perform an access check",
				string d when d.Contains("Microsoft.Skype.Sync.Pstn.Tnm.Common.Http.HttpResponseException") => "Could not connect to Teams Admin center - Tenant might be missing a Teams license",
				string e when e.Contains("Provide valid credential.") => "Error 400: There is an issue with your Exchange Token configuration. Please perform an access check for this tenant",
				_ => message,
			};
		}

		/// <summary>
		/// Obtain an access_token from refresh_token to query the Graph API
		/// </summary>
		/// <param name="tenantId">tenantId that you wish to query graph for information about</param>
		/// <param name="asApp">The query will be running as an app or delegated user?</param>
		/// <param name="appId">ID of the app that will be executing the query</param>
		/// <param name="refreshToken">refresh_token used to request access_token</param>
		/// <param name="scope">Scope of our query</param>
		/// <param name="returnRefresh">Return the refresh_token as well as the access_token?</param>
		/// <returns>A dictionary containing either the access_token or all values returned by the query for use in query headers</returns>
		public static async Task<Dictionary<string, string>> GetGraphToken(string tenantId, bool asApp, string appId = "", string refreshToken = "", string scope = "https://graph.microsoft.com//.default", bool returnRefresh = false)
		{
			string? authBody;

			if (asApp)
			{
				authBody = $"client_id={HttpUtility.UrlEncode(ApiEnvironment.Secrets.ApplicationId)}&client_secret={HttpUtility.UrlEncode(ApiEnvironment.Secrets.ApplicationSecret)}&scope={HttpUtility.UrlEncode(scope)}&grant_type=client_credentials";
			}
			else
			{
				authBody = $"client_id={ApiEnvironment.Secrets.ApplicationId}&client_secret={ApiEnvironment.Secrets.ApplicationSecret}&scope={scope}&refresh_token={ApiEnvironment.Secrets.RefreshToken}&grant_type=refresh_token";
			}

			if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(refreshToken))
			{
				authBody = $"client_id={appId}&refresh_token={refreshToken}&scope={scope}&grant_type=refresh_token";
			}

			if (string.IsNullOrEmpty(tenantId))
			{
				tenantId = ApiEnvironment.Secrets.TenantId;
			}

			if(!returnRefresh)
            {
				ApiEnvironment.AccessToken accessToken = ApiEnvironment.AccessTokenCache.Find(x => x.AppId.Equals(appId) && x.AsApp.Equals(asApp) && x.Scope.Equals(scope) && x.TenantId.Equals(tenantId));
				if (!string.IsNullOrEmpty(accessToken.Token))
                {
					if (accessToken.Expires > DateTimeOffset.Now.Subtract(new TimeSpan(3000000000)).ToUnixTimeSeconds())
					{
						return new Dictionary<string, string> { ["Authorization"] = accessToken.Token };
					}

					// Remove expired accessToken from cache
					ApiEnvironment.AccessTokenCache.Remove(accessToken);
                }
            }

			using HttpRequestMessage requestMessage = new(HttpMethod.Post, $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token");
			{
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
				requestMessage.Content = new StringContent(authBody);
				requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

				HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

				if (responseMessage.IsSuccessStatusCode)
				{

					string responseRawString = await responseMessage.Content.ReadAsStringAsync();
					string[] headersRaw = responseRawString[1..^1].Split(",");
					Dictionary<string, string> headers = new();

					foreach (string headerPairRaw in headersRaw)
					{
						string[] h = headerPairRaw.Split(":");

						if (h[0].StartsWith('"'))
						{
							// Trim the redundant " char from start and end of string
							h[0] = h[0][1..^1];
						}

						if (h[1].StartsWith('"'))
						{
							h[1] = h[1][1..^1];
						}

						headers.Add(h[0], h[1]);
					}

					if (returnRefresh)
					{
						return headers;
					}

					string accessToken = headers.GetValueOrDefault("access_token", string.Empty);
					ApiEnvironment.AccessTokenCache.Add(new() { AppId = appId, AsApp = asApp, Scope = scope, TenantId = tenantId, Token = accessToken, Expires = (await ReadJwtv1AccessDetails(accessToken)).Expires });

                    return new Dictionary<string, string> { ["Authorization"] =  accessToken};
				}
				else if(responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
				{
                    // Sleep 1 second if we get a 429 and retry
                    Console.WriteLine($"Got a 429 too many requests to GetGraphToken, waiting 1 second and retrying...");
                    Thread.CurrentThread.Join(1020);
					return await GetGraphToken(tenantId, asApp, appId, refreshToken, scope, returnRefresh);
                }

                ApiEnvironment.RunErrorCount++;

                // Write to log an error that we didn't get HTTP 2XX
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}",
                    Username = "FFPP",
                    Severity = "Error",
					Tenant = tenantId,
                    API = "GetGraphToken"
                });

                return new Dictionary<string, string> { [string.Empty] = string.Empty };
			}
		}

		/// <summary>
		/// Sends a HTTP GET request to supplied uri using graph access_token for auth
		/// </summary>
		/// <param name="uri">The url we wish to GET from</param>
		/// <param name="tenantId">The tenant the request relates to</param>
		/// <param name="scope"></param>
		/// <param name="asApp">As application or as delegated user</param>
		/// <param name="noPagination"></param>
		/// <returns>A List containing one or more JSON Elements</returns>
		public static async Task<List<JsonElement>> NewGraphGetRequest(string uri, string tenantId, string scope = "https://graph.microsoft.com//.default", bool asApp = false, bool noPagination = false)
		{
			List<JsonElement> data = new();
			Dictionary<string, string> headers;

			if (scope.ToLower().Equals("exchangeonline"))
			{
				headers = await GetGraphToken(tenantId, asApp, "a0c73c16-a7e3-4564-9a95-2bdf47383716", ApiEnvironment.Secrets.ExchangeRefreshToken, "https://outlook.office365.com/.default");

			}
			else
			{
				headers = await GetGraphToken(tenantId, asApp, string.Empty, string.Empty, scope);
			}

			FfppLogsDbContext.DebugConsoleWrite($"Using {uri} as url");

			string nextUrl = uri;

			if (await GetAuthorisedRequest(tenantId, uri))
			{
				do
				{
					try
					{
						using HttpRequestMessage requestMessage = new(HttpMethod.Get, uri);
						{
							requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", headers.GetValueOrDefault("Authorization", "FAILED-TO-GET-AUTH-TOKEN"));
							requestMessage.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");

							foreach (KeyValuePair<string, string> _h in headers)
							{
								if (!_h.Key.ToLower().Equals("authorization"))
								{
									requestMessage.Headers.TryAddWithoutValidation(_h.Key, _h.Value);
								}
							}
							HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

							if (responseMessage.IsSuccessStatusCode)
							{

								JsonDocument jsonDoc = await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()));

                                if(jsonDoc.RootElement.TryGetProperty("value", out JsonElement outValue))
                                {
									if(outValue.ValueKind == JsonValueKind.Array)
                                    {
                                        data.AddRange(jsonDoc.RootElement.GetProperty("value").EnumerateArray().ToList());
                                    }
									else
                                    {
                                        data.Add(jsonDoc.RootElement.GetProperty("value"));
                                    }
                                }
								else
                                {
                                    if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                                    {
                                        data.AddRange(jsonDoc.RootElement.EnumerateArray().ToList());
                                    }
                                    else
                                    {
                                        data.Add(jsonDoc.RootElement);
                                    }
                                }

                                nextUrl = string.Empty;

                                if (!noPagination)
								{
                                    if(jsonDoc.RootElement.TryGetProperty("@odata.nextLink",out JsonElement outNextLink))
									{
										nextUrl = outNextLink.GetString() ?? string.Empty;
									}
                                }
							}
                            else if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                // Sleep 1 second if we get a 429 and retry
                                Console.WriteLine($"Got a 429 too many requests to {uri}, waiting 1 second and retrying...");
                                Thread.CurrentThread.Join(1020);
								return await NewGraphGetRequest(uri, tenantId, scope, asApp, noPagination);
                            }
                            else
							{
                                ApiEnvironment.RunErrorCount++;
                                nextUrl = string.Empty;
                                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                                {
                                    Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}. Uri: {uri}",
                                    Username = "FFPP",
                                    Severity = "Error",
                                    Tenant = tenantId,
                                    API = "NewGraphGetRequest"
                                });
								throw new BadHttpRequestException("We did not get a http ok response from the upstream (graph)");
                            }

						}
					}
					catch (Exception ex)
					{
                        ApiEnvironment.RunErrorCount++;
                        Console.WriteLine($"Exception in NewGraphGetRequest: {ex.Message}");
						nextUrl = string.Empty;
                        throw;
					}
				}
				while (!string.IsNullOrEmpty(nextUrl));

			}
			else
			{
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = "Not allowed. You cannot manage your own tenant or tenants not under your scope",
                    Username = "FFPP",
                    Severity = "Info",
                    Tenant = tenantId,
                    API = "NewGraphGetRequest"
                });
            }

			return data;
		}

		/// <summary>
		/// Sends a HTTP GET request to supplied uri using graph access_token for auth
		/// </summary>
		/// <param name="uri">The url we wish to GET from</param>
		/// <param name="tenantId">The tenant the request relates to</param>
		/// <param name="scope"></param>
		/// <param name="asApp">As application or as delegated user</param>
		/// <param name="contentHeader">Set the content header for the type of data we want returned</param>
		/// <returns>A string representing content returned in the response</returns>
		public static async Task<string> NewGraphGetRequestString(string uri, string tenantId, string scope = "https://graph.microsoft.com//.default", bool asApp = false, string contentHeader="")
		{
			string data = string.Empty;
			Dictionary<string, string> headers;

			if (scope.ToLower().Equals("exchangeonline"))
			{
				headers = await GetGraphToken(tenantId, asApp, "a0c73c16-a7e3-4564-9a95-2bdf47383716", ApiEnvironment.Secrets.ExchangeRefreshToken, "https://outlook.office365.com/.default");

			}
			else
			{
				headers = await GetGraphToken(tenantId, asApp, string.Empty, string.Empty, scope);
			}

			FfppLogsDbContext.DebugConsoleWrite($"Using {uri} as url");

			if (await GetAuthorisedRequest(tenantId, uri))
			{
				try
				{
					using HttpRequestMessage requestMessage = new(HttpMethod.Get, uri);
					{
						requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", headers.GetValueOrDefault("Authorization", "FAILED-TO-GET-AUTH-TOKEN"));
						requestMessage.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");

						if(!string.IsNullOrEmpty(contentHeader))
                        {
							requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentHeader);

						}

						foreach (KeyValuePair<string, string> _h in headers)
						{
							if (!_h.Key.ToLower().Equals("authorization"))
							{
								requestMessage.Headers.TryAddWithoutValidation(_h.Key, _h.Value);
							}
						}
						HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

						if (responseMessage.IsSuccessStatusCode)
						{

							data = await responseMessage.Content.ReadAsStringAsync();

						}
                        else if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            // Sleep 1 second if we get a 429 and retry
                            Console.WriteLine($"Got a 429 too many requests to {uri}, waiting 1 second and retrying...");
                            Thread.CurrentThread.Join(1020);
                            return await NewGraphGetRequestString(uri, tenantId, scope, asApp, contentHeader);
                        }
                        else
						{
                            ApiEnvironment.RunErrorCount++;

                            FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                            {
                                Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}, Uri: {uri}",
                                Username = "FFPP",
                                Severity = "Error",
                                Tenant = tenantId,
                                API = "NewGraphGetRequestString"
                            });
                        }
					}
				}
				catch (Exception ex)
				{
                    ApiEnvironment.RunErrorCount++;

                    Console.WriteLine($"Exception in NewGraphGetRequest: {ex.Message}");
					throw;
				}
			}
			else
			{
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = "Not allowed. You cannot manage your own tenant or tenants not under your scope",
                    Username = "FFPP",
                    Severity = "Info",
                    Tenant = tenantId,
                    API = "NewGraphGetRequestString"
                });
            }

			return data;
		}

		/// <summary>
		/// Sends a HTTP GET request to supplied uri using graph access_token for auth
		/// </summary>
		/// <param name="uri">The url we wish to GET from</param>
		/// <param name="tenantId">The tenant the request relates to</param>
		/// <param name="scope"></param>
		/// <param name="asApp">As application or as delegated user</param>
		/// <param name="contentHeader">Set the content header for the type of data we want returned</param>
		/// <returns>A byte[] representing content returned in the response</returns>
		public static async Task<byte[]>? NewGraphGetRequestBytes(string uri, string tenantId, string scope = "https://graph.microsoft.com//.default", bool asApp = false, string contentHeader = "")
		{
			List<byte> data=new();
			Dictionary<string, string> headers;

			if (scope.ToLower().Equals("exchangeonline"))
			{
				headers = await GetGraphToken(tenantId, asApp, "a0c73c16-a7e3-4564-9a95-2bdf47383716", ApiEnvironment.Secrets.ExchangeRefreshToken, "https://outlook.office365.com/.default");

			}
			else
			{
				headers = await GetGraphToken(tenantId, asApp, string.Empty, string.Empty, scope);
			}

			FfppLogsDbContext.DebugConsoleWrite($"Using {uri} as url");

			if (await GetAuthorisedRequest(tenantId, uri))
			{
				try
				{
					using HttpRequestMessage requestMessage = new(HttpMethod.Get, uri);
					{
						requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", headers.GetValueOrDefault("Authorization", "FAILED-TO-GET-AUTH-TOKEN"));
						requestMessage.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");

						if (string.IsNullOrEmpty(contentHeader))
						{
							requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentHeader);

						}

						foreach (KeyValuePair<string, string> _h in headers)
						{
							if (!_h.Key.ToLower().Equals("authorization"))
							{
								requestMessage.Headers.TryAddWithoutValidation(_h.Key, _h.Value);
							}
						}
						HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

						if (responseMessage.IsSuccessStatusCode)
						{

							data.AddRange(await responseMessage.Content.ReadAsByteArrayAsync());

						}
                        else if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                        {
							// Sleep 1 second if we get a 429 and retry
							Console.WriteLine($"Got a 429 too many requests to {uri}, waiting 1 second and retrying...");
                            Thread.CurrentThread.Join(1020);
                            return await NewGraphGetRequestBytes(uri, tenantId, scope, asApp, contentHeader);
                        }
                        else
						{
                            ApiEnvironment.RunErrorCount++;

                            FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                            {
                                Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}. Uri: {uri}",
                                Username = "FFPP",
                                Severity = "Error",
                                Tenant = tenantId,
                                API = "NewGraphGetRequestBytes"
                            });
                        }
					}
				}
				catch (Exception ex)
				{
                    ApiEnvironment.RunErrorCount++;

                    Console.WriteLine($"Exception in NewGraphGetRequest: {ex.Message}");
					throw;
				}
			}
			else
			{
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = "Not allowed. You cannot manage your own tenant or tenants not under your scope",
                    Username = "FFPP",
                    Severity = "Info",
                    Tenant = tenantId,
                    API = "NewGraphGetRequestBytes"
                });

            }

			return data.ToArray();
		}

		/// <summary>
		/// Sends a HTTP POST or other method supplied request to supplied uri using graph access_token for auth
		/// </summary>
		/// <param name="uri">The url we wish to POST to</param>
		/// <param name="tenantId">The tenant relevant to the operation</param>
		/// <param name="body">The object we wish to send as JSON payload in body</param>
		/// <param name="type">HTTP Method POST/PUT etc.</param>
		/// <param name="scope"></param>
		/// <param name="asApp">As application or delegated user</param>
		/// <returns>The content in any response as JSON</returns>
		public static async Task<JsonElement> NewGraphPostRequest(string uri, string tenantId, object body, HttpMethod type, string scope, bool asApp)
		{
			Dictionary<string, string> headers = await GetGraphToken(tenantId, asApp, string.Empty, string.Empty, scope);

			FfppLogsDbContext.DebugConsoleWrite($"Using {uri} as url");

			if (await GetAuthorisedRequest(tenantId, uri))
			{
				try
				{
					using HttpRequestMessage requestMessage = new(type, uri);
					{
						requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", headers.GetValueOrDefault("Authorization", "FAILED-TO-GET-AUTH-TOKEN"));
						requestMessage.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");

						foreach (KeyValuePair<string, string> _h in headers)
						{
							if (!_h.Key.ToLower().Equals("authorization"))
							{
								requestMessage.Headers.TryAddWithoutValidation(_h.Key, _h.Value);
							}
						}

						requestMessage.Content = new StringContent(JsonSerializer.Serialize(body));
						requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

						HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

						if (responseMessage.IsSuccessStatusCode)
						{
							if (responseMessage.StatusCode != HttpStatusCode.NoContent)
							{
								JsonDocument jsonDoc = await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()));
								return jsonDoc.RootElement;
							}
                            else
							{
								// HTTP 204 No Content so returning empty JsonElement
								return new JsonElement();
							}
						}
                        else if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            // Sleep 1 second if we get a 429 and retry
                            Console.WriteLine($"Got a 429 too many requests to {uri}, waiting 1 second and retrying...");
                            Thread.CurrentThread.Join(1020);
                            return await NewGraphPostRequest(uri,tenantId,body,type,scope,asApp);
                        }

                        ApiEnvironment.RunErrorCount++;

                        FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                        {
                            Message = $"Incorrect HTTP status code.Expected 2XX got {responseMessage.StatusCode.ToString()}. Uri: {uri}",
                            Username = "FFPP",
                            Severity = "Error",
							Tenant = tenantId,
                            API = "NewGraphPostRequest"
                        });
                    }
				}
				catch (Exception ex)
				{
                    ApiEnvironment.RunErrorCount++;
                    Console.WriteLine($"Exception in NewGraphPostRequest: {ex.Message}");
					throw;
				}

			}
			else
			{
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = "Not allowed. You cannot manage your own tenant or tenants not under your scope",
                    Username = "FFPP",
                    Severity = "Info",
                    Tenant = tenantId,
                    API = "NewGraphPostRequest"
                });
            }

			return new JsonElement();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="tenantId"></param>
		/// <param name="resource"></param>
		/// <returns></returns>
		public static async Task<JsonElement> GetClassicApiToken(string tenantId, string resource)
		{
			string uri = string.Format("https://login.microsoftonline.com/{0}/oauth2/token", tenantId);
			string body = string.Format("resource={0}&grant_type=refresh_token&refresh_token={1}", resource, ApiEnvironment.Secrets.ExchangeRefreshToken);

			HttpRequestMessage requestMessage = new(HttpMethod.Post, uri);
			requestMessage.Content = new StringContent(body);
			requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

			HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

			if (responseMessage.IsSuccessStatusCode)
			{
				JsonDocument jsonDoc = await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()));
				return jsonDoc.RootElement;
			}
            else if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // Sleep 1 second if we get a 429 and retry
                Console.WriteLine($"Got a 429 too many requests to get classic API token, waiting 1 second and retrying...");
                Thread.CurrentThread.Join(1020);
				return await GetClassicApiToken(tenantId, resource);
            }

            ApiEnvironment.RunErrorCount++;

            FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
            {
                Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}",
                Username = "FFPP",
                Severity = "Error",
                Tenant = tenantId,
                API = "GetClassicApiToken"
            });

            return new();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="tenantId"></param>
		/// <param name="httpMethod"></param>
		/// <param name="resource"></param>
		/// <param name="contentType"></param>
		/// <param name="noPagination"></param>
		/// <returns></returns>
		public static async Task<List<JsonElement>> NewTeamsApiGetRequest(string uri, string tenantId, HttpMethod httpMethod, string resource = "48ac35b8-9aa8-4d74-927d-1f4a14a0b239", string contentType = "application/json", bool noPagination = false)
		{
			return await NewClassicApiGetRequest(uri, tenantId, httpMethod, resource, contentType, noPagination, new Dictionary<string, string> { ["x-ms-tnm-applicationid"] = "045268c0-445e-4ac1-9157-d58f67b167d9" });
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="tenantId"></param>
		/// <param name="httpMethod"></param>
		/// <param name="resource"></param>
		/// <param name="contentType"></param>
		/// <param name="noPagination"></param>
		/// <returns></returns> headers
		public static async Task<List<JsonElement>> NewClassicApiGetRequest(string uri, string tenantId, HttpMethod httpMethod, string resource = "https://admin.microsoft.com", string contentType = "application/json", bool noPagination = false, Dictionary<string, string>? headers = null)
		{
			string token = (await GetClassicApiToken(tenantId, resource)).GetProperty("access_token").ToString();

			FfppLogsDbContext.DebugConsoleWrite($"Using {uri} as url in classic API GET request");

			string nextUrl = uri;

			List<JsonElement> data = new();

			if (await GetAuthorisedRequest(tenantId, uri))
			{
				do
				{
					try
					{
						using HttpRequestMessage requestMessage = new(httpMethod, uri);
						{
							requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
							requestMessage.Headers.TryAddWithoutValidation("x-ms-client-request-id", Guid.NewGuid().ToString());
							requestMessage.Headers.TryAddWithoutValidation("x-ms-client-session-id", Guid.NewGuid().ToString());
							requestMessage.Headers.TryAddWithoutValidation("x-ms-correlation-id", Guid.NewGuid().ToString());
							requestMessage.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

							if (headers != null)
							{
								foreach (KeyValuePair<string, string> _h in headers)
								{
									if (!_h.Key.ToLower().Equals("authorization"))
									{
										requestMessage.Headers.TryAddWithoutValidation(_h.Key, _h.Value);
									}
								}
							}

							HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

							if (responseMessage.IsSuccessStatusCode)
							{
								JsonDocument jsonDoc = await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()));
								data.Add(jsonDoc.RootElement);

                                nextUrl = string.Empty;

                                if (!noPagination)
								{
                                    if( jsonDoc.RootElement.TryGetProperty("NextLink", out JsonElement outNextLink))
                                    {
										nextUrl = outNextLink.GetString() ?? string.Empty;
                                    }
                                }
							}
                            else if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                // Sleep 1 second if we get a 429 and retry
                                Console.WriteLine($"Got a 429 too many requests to get classic API token, waiting 1 second and retrying...");
                                Thread.CurrentThread.Join(1020);
                                return await NewClassicApiGetRequest(uri, tenantId, httpMethod, resource, contentType, noPagination, headers);
                            }
                            else
							{
                                ApiEnvironment.RunErrorCount++;

                                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                                {
                                    Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}. Uri: {uri}",
                                    Username = "FFPP",
                                    Severity = "Error",
                                    Tenant = tenantId,
                                    API = "NewClassicApiGetRequest"
                                });

                                nextUrl = string.Empty;
							}
						}
					}
					catch (Exception ex)
					{
                        ApiEnvironment.RunErrorCount++;

                        Console.WriteLine($"Exception in NewClassicAPIGetRequest: {ex.Message}");
						throw;
					}
				}
				while (!string.IsNullOrEmpty(nextUrl));
			}
			else
			{
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = "Not allowed. You cannot manage your own tenant or tenants not under your scope",
                    Username = "FFPP",
                    Severity = "Info",
                    Tenant = tenantId,
                    API = "NewClassicApiGetRequest"
                });
            }

			return data;
		}

		/// <summary>
        /// 
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="uri"></param>
        /// <param name="httpMethod"></param>
        /// <param name="body"></param>
        /// <param name="resource"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
		public static async Task<JsonElement> NewClassicApiPostRequest(string tenantId, string uri, HttpMethod httpMethod, object body, string resource = "https://admin.microsoft.com", Dictionary<string, string>? headers = null)
		{
			string token = (await GetClassicApiToken(tenantId, resource)).GetProperty("access_token").ToString();

			FfppLogsDbContext.DebugConsoleWrite(string.Format("Using {0} as url in classic API POST request", uri));
			
			JsonElement data = new();

			if (await GetAuthorisedRequest(tenantId, uri))
			{
				try
				{
					using HttpRequestMessage requestMessage = new(httpMethod, uri);
					{
						requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
						requestMessage.Headers.TryAddWithoutValidation("x-ms-client-request-id", Guid.NewGuid().ToString());
						requestMessage.Headers.TryAddWithoutValidation("x-ms-client-session-id", Guid.NewGuid().ToString());
						requestMessage.Headers.TryAddWithoutValidation("x-ms-correlation-id", Guid.NewGuid().ToString());
						requestMessage.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

						if (headers != null)
						{
							foreach (KeyValuePair<string, string> _h in headers)
							{
								if (!_h.Key.ToLower().Equals("authorization"))
								{
									requestMessage.Headers.TryAddWithoutValidation(_h.Key, _h.Value);
								}
							}
						}

						requestMessage.Content = new StringContent(JsonSerializer.Serialize(body));
						requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

						HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

						if (responseMessage.IsSuccessStatusCode)
						{
							JsonDocument jsonDoc = await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()));
							data = jsonDoc.RootElement;
						}
                        else if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            // Sleep 1 second if we get a 429 and retry
                            Console.WriteLine($"Got a 429 too many requests to get {uri}, waiting 1 second and retrying...");
                            Thread.CurrentThread.Join(1020);
                            return await NewClassicApiPostRequest(tenantId, uri, httpMethod, body, resource, headers);
                        }
                        else
						{
                            ApiEnvironment.RunErrorCount++;

                            FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                            {
                                Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}, Uri: {uri}",
                                Username = "FFPP",
                                Severity = "Error",
                                Tenant = tenantId,
                                API = "NewClassicApiPostRequest"
                            });
                        }
					}
				}
				catch (Exception ex)
				{
                    ApiEnvironment.RunErrorCount++;

                    Console.WriteLine($"Exception in NewClassicApiPostRequest: {ex.Message}");
					throw;
				}
			}
			else
			{
				FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = "Not allowed. You cannot manage your own tenant or tenants not under your scope",
                    Username = "FFPP",
                    Severity = "Info",
                    Tenant = tenantId,
                    API = "NewClassicApiPostRequest"
                });
			}

			return data;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="tenantId"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		public static async Task<JsonElement> NewExoRequest(string tenantId, string cmdLet, object? cmdParams = null)
		{

			CmdletBody exoBody = new() { CmdletInput = new() { CmdletName = cmdLet, Parameters = cmdParams ?? JsonSerializer.Deserialize<JsonElement>(@"{}") } };

			string token = (await GetClassicApiToken(tenantId, "https://outlook.office365.com")).GetProperty("access_token").ToString();
			JsonElement returnData = new();

			if (await GetAuthorisedRequest(tenantId))
			{
				string tenant = (await Tenant.GetTenants(string.Empty)).Find(x => x.defaultDomainName.Equals(tenantId)).customerId ?? string.Empty;
				string onMicrosoft = (await NewGraphGetRequest("https://graph.microsoft.com/beta/domains?$top=999", tenantId)).Find(x => x.GetProperty("isInitial").GetBoolean().Equals(true)).GetProperty("id").ToString();
				string uri = string.Format("https://outlook.office365.com/adminapi/beta/{0}/InvokeCommand", tenant);
				HttpRequestMessage requestMessage = new (HttpMethod.Post, uri);
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
				requestMessage.Headers.TryAddWithoutValidation("X-AnchorMailbox", @"UPN:SystemMailbox{bb558c35-97f1-4cb9-8ff7-d53741dc928c}@"+onMicrosoft);

				try
				{
					requestMessage.Content = new StringContent(JsonSerializer.Serialize(exoBody));
					requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

					HttpResponseMessage responseMessage = await SendHttpRequest(requestMessage);

					if (responseMessage.IsSuccessStatusCode)
					{
						JsonDocument jsonDoc = await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()));
						returnData = jsonDoc.RootElement.GetProperty("value");
					}
                    else if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        // Sleep 1 second if we get a 429 and retry
                        Console.WriteLine($"Got a 429 too many requests NewExoRequest ({cmdLet}), waiting 1 second and retrying...");
                        Thread.CurrentThread.Join(1020);
						return await NewExoRequest(tenantId, cmdLet, cmdParams);
                    }

                    ApiEnvironment.RunErrorCount++;

                    FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                    {
                        Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}, Uri: {uri}",
                        Username = "FFPP",
                        Severity = "Error",
                        Tenant = tenantId,
                        API = "NewExoRequest"
                    });
                }
				catch (Exception ex)
				{
                    ApiEnvironment.RunErrorCount++;
                    Console.WriteLine($"Exception in NewExoRequest: {ex.Message}");
					throw;
				}
			}
			else
			{
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = "Not allowed. You cannot manage your own tenant or tenants not under your scope",
                    Username = "FFPP",
                    Severity = "Info",
                    Tenant = tenantId,
                    API = "NewExoRequest"
                });
            }

			return returnData;
		}

		/// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="scope"></param>
        /// <param name="firstLogon"></param>
        /// <param name="device_code"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
		public static async Task<JsonElement> NewDeviceLogin(string clientId, string scope, bool firstLogon ,string device_code, string tenantId)
		{
			string encodedscope = Uri.EscapeDataString(scope);
			string uri = "https://login.microsoftonline.com/organizations/oauth2/v2.0/token";
			HttpRequestMessage requestMessage;
			HttpResponseMessage responseMessage;

			if (firstLogon)
			{
				if (!string.IsNullOrEmpty(tenantId))
				{
					uri = string.Format("https://login.microsoftonline.com/{0}/oauth2/v2.0/devicecode", tenantId);
				}
				else
				{
					uri = "https://login.microsoftonline.com/organizations/oauth2/v2.0/devicecode";
				}

				requestMessage = new(HttpMethod.Post, uri);
				requestMessage.Content = new StringContent(string.Format("client_id={0}&scope={1}+offline_access+profile+openid", clientId, encodedscope));
				requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

				responseMessage = await SendHttpRequest(requestMessage);

                if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    // Sleep 1 second if we get a 429 and retry
                    Console.WriteLine($"Got a 429 too many requests to NewDeviceLogin, waiting 1 second and retrying...");
                    Thread.CurrentThread.Join(1020);
					return await NewDeviceLogin(clientId, scope, firstLogon, device_code, tenantId);
                }
                else if(!responseMessage.IsSuccessStatusCode)
                {
                    ApiEnvironment.RunErrorCount++;

                    FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                    {
                        Message = $"Incorrect HTTP status code. Expected 2XX got {responseMessage.StatusCode.ToString()}, Uri: {uri}",
                        Username = "FFPP",
                        Severity = "Error",
                        Tenant = tenantId,
                        API = "NewDeviceLogin"
                    });
                }
			}
			else
			{
				requestMessage = new(HttpMethod.Post, uri);
				requestMessage.Content = new StringContent($"client_id={clientId}&scope={encodedscope}+offline_access+profile+openid&grant_type=device_code&device_code={device_code}");
				requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

				responseMessage = await SendHttpRequest(requestMessage);

				if (!(await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()))).RootElement.TryGetProperty("refresh_token", out JsonElement refreshToken))
				{

					return (await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()))).RootElement.GetProperty("error");
				}
			}

			return (await JsonDocument.ParseAsync(new MemoryStream(await responseMessage.Content.ReadAsByteArrayAsync()))).RootElement;
		}

		/// <summary>
		/// Used to describe a JWT v1 Token
		/// </summary>
		public struct TokenDetails
		{
			public TokenDetails(string appId = "", string appName = "", string audience = "", string authMethods = "", string iPAddress = "", string name = "", string scope = "", string tenantId = "", string userPrincipleName = "", string roles = "", long exp = 0, string token = "")
			{
				AppId = appId;
				AppName = appName;
				Audience = audience;
				AuthMethods = authMethods;
				IpAddress = iPAddress;
				Name = name;
                Roles = roles;
				ScopeString = scope;
                Scope = scope.Split(' ');
				TenantId = tenantId;
				UserPrincipalName = userPrincipleName;
				Expires = exp;
				AccessToken = token;
				 
			}

			public string AppId { get; }
			public string AppName { get; }
			public string Audience { get; }
			public string AuthMethods { get; }
			public string IpAddress { get; }
			public string Name { get; }
            public string Roles { get; }
			public string ScopeString { get; }
            public string[] Scope { get; }
			public string TenantId { get; }
			public string UserPrincipalName { get; }
			public long Expires { get; }
			public string AccessToken { get; }
        }

		/// <summary>
		/// Converts a JWT v1 token into a JSON object
		/// </summary>
		/// <param name="token">Token to decode</param>
		/// <returns>JSON object representing the token</returns>
		public static async Task<TokenDetails> ReadJwtv1AccessDetails(string token)
		{


			if (!token.Contains('.') || !token.StartsWith("eyJ"))
			{
				return new TokenDetails();
			}

			byte[] tokenPayload = Utilities.Base64UrlDecode(token.Split('.')[1]);
			string appName = string.Empty;
			string upn = string.Empty;
			string amr = string.Empty;
            string ipaddr = string.Empty;
            string name = string.Empty;
            string scp = string.Empty;
            string roles = string.Empty;

            JsonElement jsonToken = (await JsonDocument.ParseAsync(new MemoryStream(tokenPayload))).RootElement;

			if (jsonToken.TryGetProperty("app_displayname", out JsonElement appNameJson))
			{
				appName = appNameJson.GetString() ?? string.Empty;
			}

			if (jsonToken.TryGetProperty("upn", out JsonElement upnJson))
			{
				upn = upnJson.GetString() ?? string.Empty;
			}
			else if (jsonToken.TryGetProperty("unique_name", out upnJson))
			{
				upn = upnJson.GetString() ?? string.Empty;
			}

			if (jsonToken.TryGetProperty("amr", out JsonElement amrJson))
			{
				amr = jsonToken.GetProperty("amr").ToString();
            }

            if (jsonToken.TryGetProperty("ipaddr", out JsonElement ipaddrJson))
            {
                ipaddr = ipaddrJson.GetString() ?? string.Empty;
            }

            if (jsonToken.TryGetProperty("name", out JsonElement nameJson))
            {
                name = nameJson.GetString() ?? string.Empty;
            }

            if (jsonToken.TryGetProperty("scp", out JsonElement scpJson))
            {
                scp = scpJson.GetString() ?? string.Empty;
            }

            if (jsonToken.TryGetProperty("roles", out JsonElement rolesJson))
            {
                roles = jsonToken.GetProperty("roles").ToString() ?? string.Empty;
            }

            return new(jsonToken.GetProperty("appid").ToString(), appName,
				jsonToken.GetProperty("aud").ToString(),amr, ipaddr,
				name, scp, jsonToken.GetProperty("tid").ToString(), upn, roles, long.Parse(jsonToken.GetProperty("exp").ToString()));
		}

		/// <summary>
		/// Uses the HttpClient attached to this class to send HTTP request
		/// </summary>
		/// <param name="requestMessage">HttpRequestMessage to send</param>
		/// <returns>HttpResponseMessage is returned</returns>
		public static async Task<HttpResponseMessage> SendHttpRequest(HttpRequestMessage requestMessage)
        {
			return await _httpClient.SendAsync(requestMessage);
		}

		public struct CmdletBody
        {
			public CmdletInput CmdletInput { get; set; }
		}

		public struct CmdletInput
        {
			public string CmdletName { get; set; }
			public object Parameters { get; set;  }
        }

		// Makes sure we are authorised to access this tenant
		private async static Task<bool> GetAuthorisedRequest(string tenantId, string uri = "")
		{
			if (uri.ToLower().Contains("https://graph.microsoft.com/beta/contracts") || uri.ToLower().Contains("/customers/") ||
                uri.ToLower().Contains("https://graph.microsoft.com/v1.0/serviceprincipals") ||
                uri.ToLower().Contains("https://graph.microsoft.com/v1.0/groups") ||
                uri.ToLower().Equals("https://graph.microsoft.com/v1.0/me/sendmail") ||
				uri.ToLower().Contains("https://graph.microsoft.com/beta/tenantrelationships/managedtenants") ||
				(uri.ToLower().Contains("https://graph.microsoft.com/v1.0/applications") && tenantId.Equals(ApiEnvironment.Secrets.TenantId)) ||
				(uri.ToLower().Contains("https://graph.microsoft.com/beta/domains") && tenantId.Equals(ApiEnvironment.Secrets.TenantId)) ||
				uri.ToLower().Contains("/photos/48x48/$value") || uri.ToLower().Contains("/photos/120x120/$value") ||
				uri.ToLower().Contains("/photos/240x240/$value") || uri.ToLower().Contains("/photos/432x432/$value") ||
				uri.ToLower().Contains("/photos/648x648/$value") ||
				(uri.ToLower().StartsWith("https://graph.microsoft.com/beta/users/") && uri.ToLower().EndsWith("?$top=999&$select=usagelocation")))
			{
				return true;
			}

			List<Tenant> tenants = await Tenant.GetTenants(string.Empty);

			if (tenants != null && tenants.Count > 0)
			{
				// Check if tenantId exists in any of the properties of our allowed tenants
				if (tenants.Find(x => x.defaultDomainName.ToLower().Equals(tenantId.ToLower())) != null ||
					tenants.Find(x => x.customerId.ToLower().Equals(tenantId.ToLower())) != null ||
					tenants.Find(x => x.displayName.ToLower().Equals(tenantId.ToLower())) != null)
				{
					return true;
				}
			}

			return false;
		}
    }
}