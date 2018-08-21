using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Diagnostics;
using Microsoft.Azure;
using System.Collections.Generic;
using UsageToOMSCore;

namespace UsageToOMSFunc
{
    public static class UsageToOMS
    {        
        [FunctionName("UsageToOMS")]
        public static void Run([TimerTrigger("0 0 */4 * * *")]TimerInfo myTimer, TraceWriter log)        
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");            
            try
            {               
                string omsworkspaceid = CryptoHelper.GetKeyVaultSecret("omsworkspaceid");
                string omsworkspacekey = CryptoHelper.GetKeyVaultSecret("omsworkspacekey");  
                if(string.IsNullOrEmpty(omsworkspaceid))
                {
                    log.Info($"OmsWorkspaceId is empty. Cannot proceed further");
                    return;
                }

                if (string.IsNullOrEmpty(omsworkspacekey))
                {
                    log.Info($"omsworkspacekey is empty. Cannot proceed further");
                    return;
                }

                log.Info($"Processing started at {DateTime.UtcNow.ToString()}");                
                OmsIngestionProcessor.StartIngestion(omsworkspaceid, omsworkspacekey, log);
                log.Info($"Finished processing at  {DateTime.UtcNow.ToString()}");               
            }
            catch (Exception ex)
            {                
                log.Info($"Error: {ex}");
                throw ex;
            }            
        }
    }
}
