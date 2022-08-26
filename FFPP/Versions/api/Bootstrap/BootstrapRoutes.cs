using FFPP.Data;
using FFPP.Common;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace FFPP.Api.Bootstrap
{
    /// <summary>
    /// /api is basically a skeleton set of routes that always point to the latest /vX.Y api
    /// </summary>
    public static class BootstrapRoutes
    {
        private static string _deviceFetchCode = string.Empty;

        public static void InitRoutes(ref WebApplication app)
        {
            /// <summary>
            /// /bootstrap/TokenStatus
            /// </summary>
            app.MapGet("/bootstrap/TokenStatus", async () =>
            {
                Task<ApiEnvironment.ApiTokenStatus> getTokenStatus = new(() =>
                { 
                    return new ApiEnvironment.ApiTokenStatus();
                });

                getTokenStatus.Start();
                return await getTokenStatus;

            }).WithName("/bootstrap/TokenStatus").ExcludeFromDescription();

            /// <summary>
            /// /bootstrap/GetGraphTokenUrl
            /// </summary>
            app.MapGet("/bootstrap/GetGraphTokenUrl", async (HttpContext context) =>
            {
                if (!ApiEnvironment.IsBoostrapped)
                {
                    Task<GraphTokenUrl> getUrl = new(() =>
                    {
                        return new() { url = $"https://login.microsoftonline.com/{ApiEnvironment.Secrets.TenantId}/oauth2/v2.0/authorize?scope=https://graph.microsoft.com/.default+offline_access+openid+profile&response_type=code&client_id={ApiEnvironment.Secrets.ApplicationId}&redirect_uri={ApiEnvironment.FfppFrontEndUri}/bootstrap/receivegraphtoken" };
                    });

                    getUrl.Start();
                    return await getUrl;
                }
                else
                {
                    context.Response.StatusCode = 410;
                    return new();
                }

            }).WithName("/bootstrap/GetGraphTokenUrl").ExcludeFromDescription();

            /// <summary>
            /// /bootstrap/ReceiveGraphToken
            /// </summary>
            app.MapGet("/bootstrap/ReceiveGraphToken", async (HttpContext context, HttpRequest request, string code) =>
            {
                if (!ApiEnvironment.IsBoostrapped)
                {
                    HttpRequestMessage requestMessage = new(HttpMethod.Post, $"https://login.microsoftonline.com/{ApiEnvironment.Secrets.TenantId}/oauth2/v2.0/token");
                    requestMessage.Content = new StringContent($"client_id={ApiEnvironment.Secrets.ApplicationId}&scope=https://graph.microsoft.com/.default+offline_access+openid+profile&code={code}&redirect_uri={ApiEnvironment.FfppFrontEndUri}/bootstrap/receivegraphtoken&grant_type=authorization_code&client_secret={ApiEnvironment.Secrets.ApplicationSecret}");

                    requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                    HttpResponseMessage responseMessage = await RequestHelper.SendHttpRequest(requestMessage);

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

                        ApiEnvironment.Secrets.RefreshToken = headers["refresh_token"];
                        ApiZeroConfiguration zeroConf = await ApiZeroConfiguration.Read();
                        zeroConf.RefreshToken = ApiEnvironment.Secrets.RefreshToken;
                        zeroConf.Save();
                        //redirect to success page
                        context.Response.Redirect($"{ApiEnvironment.FfppFrontEndUri}/setup/graphtoken/success");
                    }
                    else
                    {
                        //redirect to error page
                        context.Response.Redirect($"{ApiEnvironment.FfppFrontEndUri}/setup/graphtoken/error");
                    }
                }
                else
                {
                    context.Response.StatusCode = 410;
                }

            }).WithName("/bootstrap/ReceiveGraphToken").ExcludeFromDescription();

            /// <summary>
            /// /bootstrap/GetExchangeTokenUrlCode
            /// </summary>
            app.MapGet("/bootstrap/GetExchangeTokenUrlCode", async (HttpContext context) =>
            {
                if (!ApiEnvironment.IsBoostrapped)
                {

                    JsonElement deviceLoginInfo = await RequestHelper.NewDeviceLogin("a0c73c16-a7e3-4564-9a95-2bdf47383716", "https://outlook.office365.com/.default", true, string.Empty, ApiEnvironment.Secrets.TenantId);
                    DeviceLogin response = new() { url = deviceLoginInfo.GetProperty("verification_uri").GetString(), code = deviceLoginInfo.GetProperty("user_code").GetString(), expires = deviceLoginInfo.GetProperty("expires_in").GetInt32() };

                    string deviceCode = deviceLoginInfo.GetProperty("device_code").GetString();
                    int interval = deviceLoginInfo.GetProperty("interval").GetInt32();
                    int expires = deviceLoginInfo.GetProperty("expires_in").GetInt32();

                    Task fetchDeviceCodeLogin = new(() =>
                    {
                        WaitForDeviceCodeLogin(deviceCode, interval, expires);
                    });

                    fetchDeviceCodeLogin.Start();

                    return response;
                }
                else
                {
                    context.Response.StatusCode = 410;
                    return new();
                }

            }).WithName("/bootstrap/GetExchangeTokenUrlCode").ExcludeFromDescription();
        }

        private static async void WaitForDeviceCodeLogin(string deviceCode, int interval, int expires)
        {
            int rounds = expires / interval;

            for (int i=1; i<=rounds; i++)
            {
                Console.WriteLine($"Checking for Exchange token using device_code: {deviceCode} - ({i}/{rounds})");
                JsonElement finalCode = await RequestHelper.NewDeviceLogin("a0c73c16-a7e3-4564-9a95-2bdf47383716", "https://outlook.office365.com/.default", false, deviceCode, ApiEnvironment.Secrets.TenantId);
                if (finalCode.ValueKind != JsonValueKind.String)
                {
                    try
                    {
                        ApiEnvironment.Secrets.ExchangeRefreshToken = finalCode.GetProperty("refresh_token").GetString();
                        ApiZeroConfiguration zeroConf = await ApiZeroConfiguration.Read();
                        zeroConf.ExchangeRefreshToken = ApiEnvironment.Secrets.ExchangeRefreshToken;
                        zeroConf.IsBootstrapped = true;
                        zeroConf.Save();
                        break;
                    }
                    catch(Exception ex)
                    {
                        ApiEnvironment.RunErrorCount++;

                        Console.WriteLine($"Error getting Exchange Refresh Token: {ex.Message}");
                    }
                }
                Thread.CurrentThread.Join(interval * 1000);
            }
        }

        public struct DeviceLogin
        {
            public string url { get; set; }
            public string code { get; set; }
            public int expires { get; set; }
        }

        public struct GraphTokenUrl
        {
            public string url { get; set; }
        }
    }
}