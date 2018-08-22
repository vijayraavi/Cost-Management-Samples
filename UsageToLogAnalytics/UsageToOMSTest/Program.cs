using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using UsageToOMSCore;
using  Utilities;

namespace UsageToOMSFuncTest
{
    class Program
    {
        public static void Main(string[] args)
        {
            var logger = new TraceWriterStub(TraceLevel.Info);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            TimeSpan ts = stopWatch.Elapsed;
            OmsIngestionProcessor.StartIngestion(logger).Wait();
            stopWatch.Stop();
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine(elapsedTime);
            logger.Info($"Finished processing at  {DateTime.UtcNow.ToString()}. Total time took is {elapsedTime}");
        }
    }

    public class TraceWriterStub : TraceWriter
    {
        protected TraceLevel _level;
        protected List<TraceEvent> _traces;

        public TraceWriterStub(TraceLevel level) : base(level)
        {
            _level = level;
            _traces = new List<TraceEvent>();
        }

        public override void Trace(TraceEvent traceEvent)
        {
            _traces.Add(traceEvent);
        }

        public List<TraceEvent> Traces => _traces;
    }
}
