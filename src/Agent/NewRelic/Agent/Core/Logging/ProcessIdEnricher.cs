using NewRelic.SystemInterfaces;
using Serilog.Core;
using Serilog.Events;

namespace NewRelic.Agent.Core
{
    class ProcessIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "pid", new ProcessStatic().GetCurrentProcess().Id));
        }
    }
}