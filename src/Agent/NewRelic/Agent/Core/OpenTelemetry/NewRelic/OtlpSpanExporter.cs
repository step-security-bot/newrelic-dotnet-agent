// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using Grpc.Core;
using NewRelic.Agent.Configuration;
using NewRelic.Agent.Core.Segments;
using NewRelic.Core.Logging;
using OtlpCollector = Opentelemetry.Proto.Collector.Trace.V1;
using OtlpCommon = Opentelemetry.Proto.Common.V1;
using OtlpResource = Opentelemetry.Proto.Resource.V1;
using OtlpTrace = Opentelemetry.Proto.Trace.V1;
using static Opentelemetry.Proto.Collector.Trace.V1.TraceService;
using NewRelic.Agent.Core;

namespace OpenTelemetry.NewRelic
{
    public class OtlpSpanExporter : BaseExporter<ISpanEventWireModel>
    {
        private readonly Channel _channel;
        private readonly TraceServiceClient _traceServiceClient;
        private readonly IConfigurationService _configurationService;

        public OtlpSpanExporter(IConfigurationService configurationService)
        {
            var endpoint = GetEndpoint();

            ChannelCredentials channelCredentials;
            if (endpoint.Scheme == Uri.UriSchemeHttps)
            {
                channelCredentials = new SslCredentials();
            }
            else
            {
                channelCredentials = ChannelCredentials.Insecure;
            }

            _channel = new Channel(endpoint.Authority, channelCredentials);
            _traceServiceClient = new TraceServiceClient(_channel);
            _configurationService = configurationService;
        }

        public override ExportResult Export(in Batch<ISpanEventWireModel> batch)
        {
            try
            {
                var request = CreateExportTraceServiceRequest(batch);
                _traceServiceClient.Export(request);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to export spans {ex}");
                return ExportResult.Failure;
            }
            return ExportResult.Success;
        }

        private Uri GetEndpoint()
        {
            var endpoint = System.Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT");
            try
            {
                return new Uri(endpoint);
            }
            catch
            {
                Log.Error($"Invalid Uri configured for OTEL_EXPORTER_OTLP_TRACES_ENDPOINT = {endpoint}. Defaulting to http://localhost:4317.");
                return new Uri("http://localhost:4317");
            }
        }

        private OtlpResource.Resource _processResource;
        private OtlpResource.Resource ProcessResource => _processResource ?? (_processResource = CreateProcessResource());

        private OtlpResource.Resource CreateProcessResource()
        {
            var resource = new OtlpResource.Resource();
            resource.SetAttribute("agent_run_token", _configurationService.Configuration.AgentRunId.ToString());
            resource.SetAttribute("service.name", _configurationService.Configuration.ApplicationNames.First());
            foreach (var item in _configurationService.Configuration.RequestHeadersMap)
            {
                resource.SetAttribute(item.Key, item.Value);
            }
            return resource;
        }

        private OtlpCollector.ExportTraceServiceRequest CreateExportTraceServiceRequest(Batch<ISpanEventWireModel> wireModels)
        {
            var request = new OtlpCollector.ExportTraceServiceRequest();

            var resourceSpans = new OtlpTrace.ResourceSpans
            {
                Resource = ProcessResource,
            };

            request.ResourceSpans.Add(resourceSpans);

            var instrumentationLibrarySpans = new OtlpTrace.InstrumentationLibrarySpans
            {
                InstrumentationLibrary = new OtlpCommon.InstrumentationLibrary
                {
                    Name = "New Relic .NET Agent",
                    Version = AgentInstallConfiguration.AgentVersion,
                },
            };

            resourceSpans.InstrumentationLibrarySpans.Add(instrumentationLibrarySpans);

            foreach (var item in wireModels)
            {
                var span = item.ToOtlpSpan();
                instrumentationLibrarySpans.Spans.Add(span);
            }

            return request;
        }
    }
}
