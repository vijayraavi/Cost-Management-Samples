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
        private static readonly string _enrollmentNumber = "8099099";
        static string baseurl = "https://consumption.azure.com/v2/enrollments";

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
            HttpClient client = HttpHandler.BuildClient();
            int count = ProcessQuery(oms, client, GetUsageQueryUrl(), log);
            log.Info($"Finished processing files");
        }

        public static int ProcessQuery(OMSIngestionApi oms, HttpClient client, String query, TraceWriter log)
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
                Task.WaitAll(tasks.ToArray());
                log.Info($"Final Record Count {count}");
                return count;
            }
            catch (Exception e)
            {
                log.Info($"Failed processing. Reason: {e}");
                throw e;
            }
        }

        public static string GetUsageQueryUrl()
        {
            DateTime startDate = DateTime.Now.AddMonths(-35);
            DateTime endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            return $"{baseurl}/{_enrollmentNumber}/usagedetailsbycustomdate?startTime={startDate.ToShortDateString()}&endTime={endDate.ToShortDateString()}";
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
