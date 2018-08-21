using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace UsageToOMSCore
{
    public static class OmsIngestionProcessor
    {         
        private static List<string> auditLogProcessingFailures = new List<string>();      
        private static string workspaceid = "";
        private static string workspacekey = "";
        private readonly static string _enrollmentNumber = "8099099";
        private readonly static bool _includeHeader = true;
        static string baseurl = "https://consumption.azure.com/v2/enrollments";
        public static AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

        public static void StartIngestion(string omsWorkspaceId, string omsWorkspaceKey, TraceWriter log)
        {
            workspaceid = omsWorkspaceId;
            workspacekey = omsWorkspaceKey;
            if (string.IsNullOrEmpty(workspaceid))
            {
                log.Info($"OmsWorkspaceId is empty. Cannot proceed further");
                return;
            }

            if (string.IsNullOrEmpty(workspacekey))
            {
                log.Info($"omsworkspacekey is empty. Cannot proceed further");
                return;
            }
            log.Info("Sending logs to OMS");
            var oms = new OMSIngestionApi(workspaceid, workspacekey, log);
            HttpClient client = BuildClient();
            int count = ProcessQuery(oms, client, GetUsageQueryUrl(), log);
            log.Info($"Finished processing files");
        }

        public static int ProcessQuery(OMSIngestionApi oms, HttpClient client, String query, TraceWriter log)
        {
            try
            {
                JsonResult<UCDDHourly> jsonResult = default(JsonResult<UCDDHourly>);
                int count = 0;
                var tasks = new List<Task>();
                while (!string.IsNullOrEmpty(query))
                {
                    var response = client.GetAsync(new Uri(query)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        jsonResult = JsonConvert.DeserializeObject<JsonResult<UCDDHourly>>(result);
                        var jsonList = JsonConvert.SerializeObject(jsonResult.data.ToList());
                        tasks.Add(oms.SendOMSApiIngestionFile(jsonList));
                        count = count + jsonResult.data.ToList().Count;
                        Console.WriteLine($"Count {count}");
                        query = jsonResult.nextLink;
                    }
                    else
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        log.Info(result.ToString());
                        break;
                    }
                }
                Task.WaitAll(tasks.ToArray());
                log.Info($"Record Count {count}");
                return count;
            }
            catch (Exception e)
            {
                log.Info($"Failed processing. Reason: {e}");
                throw e;
            }
        }
       
        public static async Task<String> GetAuthorizationHeader()
        {
            var token = await GetToken();
            return $"Bearer {token}";
        }

        public static async Task<string> GetToken()
        {
            string secretUri = String.Format("{0}{1}", CloudConfigurationManager.GetSetting("KeyVaultURL"), CloudConfigurationManager.GetSetting("accesstokensecretname"));
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var result = await keyVaultClient.GetSecretAsync(secretUri);
            return result.Value;
        }

        public static string GetUsageQueryUrl()
        {
            DateTime startDate = DateTime.Now.AddMonths(-35);
            DateTime endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            return $"{baseurl}/{_enrollmentNumber}/usagedetailsbycustomdate?startTime={startDate.ToShortDateString()}&endTime={endDate.ToShortDateString()}";
        }

        public static WebRequestHandler SetHandler()
        {
            var handler = new WebRequestHandler();
            return handler;
        }

        public static HttpClient SetClient(WebRequestHandler handler)
        {
            HttpClient httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            var activityid = Guid.NewGuid().ToString();
            //activityid.Dump();
            if (_includeHeader)
            {
                httpClient.DefaultRequestHeaders.Add("x-ms-correlation-id", activityid);
                httpClient.DefaultRequestHeaders.Add("x-ms-tracking-id", Guid.NewGuid().ToString());
                string authHeader = GetAuthorizationHeader().Result;
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            }

            return httpClient;
        }
        
        public static HttpClient BuildClient()
        {
            var handler = SetHandler();
            var httpClient = SetClient(handler);
            return httpClient;
        }       
    }
    public class JsonResult<T>
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("nextLink")]
        public string nextLink { get; set; }

        [JsonProperty("data")]
        public T[] data { get; set; }
    }
}
