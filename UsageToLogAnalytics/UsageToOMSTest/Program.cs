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

namespace UsageToOMSFuncTest
{
    class Program
    {
        public static void Main(string[] args)
        {
            string worskpaceid = ""; // put your workspaceid here. Its found in Advanced Settings on Log Analytics page
            string workspacekey = ""; // put your workspacekey here. Its found in Advanced Settings on Log Analytics page
            if (string.IsNullOrEmpty(worskpaceid))
            {
                Console.WriteLine($"OmsWorkspaceId is empty. Cannot proceed further");
                return;
            }

            if (string.IsNullOrEmpty(workspacekey))
            {
                Console.WriteLine($"omsworkspacekey is empty. Cannot proceed further");
                return;
            }
            var logger = new TraceWriterStub(TraceLevel.Info);
            OmsIngestionProcessor.StartIngestion(worskpaceid, workspacekey, logger);
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
