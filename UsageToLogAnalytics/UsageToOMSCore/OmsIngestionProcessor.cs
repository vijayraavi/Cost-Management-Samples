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
using Utilities;

namespace UsageToOMSCore
{
    public static class OmsIngestionProcessor
    {        
        private static string workspaceid = "";
        private static string workspacekey = "";
        private static readonly string _enrollmentNumber = CloudConfigurationManager.GetSetting("EnrollmentNumber");
        static string baseurl = CloudConfigurationManager.GetSetting("AzureBillingUrl");

        public static async Task StartIngestion(TraceWriter log)
        {
            //Save your workspaceid and workspacekey in KeyVault
            workspaceid = CryptoHelper.GetKeyVaultSecret("omsworkspaceid");
            workspacekey = CryptoHelper.GetKeyVaultSecret("omsworkspacekey");
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
            HttpClient client = HttpHandler.BuildClient();
            int count = await ProcessQuery(oms, client, GetUsageQueryUrl(log), log);
            log.Info($"Finished processing files");
        }

        private static async Task<int> ProcessQuery(OMSIngestionApi oms, HttpClient client, String query, TraceWriter log)
        {
            try
            {
                int count = 0;
                var tasks = new List<Task>();
                while (!string.IsNullOrEmpty(query))
                {
                    var response = client.GetAsync(new Uri(query)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        JsonResult<UCDDHourly> jsonResult = default(JsonResult<UCDDHourly>);
                        jsonResult = JsonConvert.DeserializeObject<JsonResult<UCDDHourly>>(result);
                        var jsonList = JsonConvert.SerializeObject(jsonResult.data.ToList());
                        tasks.Add(oms.SendOMSApiIngestionFile(jsonList));
                        count = count + jsonResult.data.ToList().Count;
                        Console.WriteLine($"Count {count}");
                        log.Info($"Count {count}");
                        query = jsonResult.nextLink;
                    }
                    else
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        log.Info(result);
                        break;
                    }
                }
               await Task.WhenAll(tasks.ToArray());
                log.Info($"Final Record Count {count}");
                return count;
            }
            catch (Exception e)
            {
                log.Info($"Failed processing. Reason: {e}");
                throw e;
            }
        }

        public static string GetUsageQueryUrl(TraceWriter log)
        {
            //Getting data for current month. You can change the dates as you like.
            //Please refer to this https://docs.microsoft.com/en-us/rest/api/billing/enterprise/billing-enterprise-api-usage-detail
            //and this https://docs.microsoft.com/en-us/azure/billing/billing-enterprise-api
            //Doing this to load last slice of data for last day of month which spills into next month's first day
            //For ex:The usage for Aug 31 for 11:30 pm is available on Sep 1st.
            var today = DateTime.UtcNow;
            today = (today.Day == 1 ? today.AddDays(-1) : today);
            DateTime startDate = new DateTime(today.Year, today.Month, 1);
            DateTime endDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
            string url =
                $"{baseurl}/{_enrollmentNumber}/usagedetailsbycustomdate?startTime={startDate.ToShortDateString()}&endTime={endDate.ToShortDateString()}";
            log.Info($"Queried for url: {url}");
            return url;
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