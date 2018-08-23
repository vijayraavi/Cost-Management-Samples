using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Diagnostics;
using Microsoft.Azure;
using System.Collections.Generic;
using System.Threading;
using UsageToOMSCore;
using Utilities;

namespace UsageToOMSFunc
{
    public static class UsageToOMS
    {        
        [FunctionName("UsageToOMS")]
        //Change the schedule as you wish. Here its set to run at 3 am every day
        // Sample cron expressions.https://codehollow.com/2017/02/azure-functions-time-trigger-cron-cheat-sheet/
        public static void Run([TimerTrigger("0 0 3 * * *")]TimerInfo myTimer, TraceWriter log)        
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.UtcNow}");            
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                TimeSpan ts = stopWatch.Elapsed;
                log.Info($"Processing started at {DateTime.UtcNow.ToString()}");                
                OmsIngestionProcessor.StartIngestion(log).Wait();
                stopWatch.Stop();
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",ts.Hours, ts.Minutes, ts.Seconds,ts.Milliseconds / 10);
                log.Info($"Finished processing at  {DateTime.UtcNow.ToString()}. Total processing time is {elapsedTime}");               
            }
            catch (Exception ex)
            {                
                log.Info($"Error: {ex}");
                throw ex;
            }            
        }
    }
}
