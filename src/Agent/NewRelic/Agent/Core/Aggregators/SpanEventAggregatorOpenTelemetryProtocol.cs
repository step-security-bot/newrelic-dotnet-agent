// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Google.Protobuf;
using Grpc.Core;
using NewRelic.Agent.Configuration;
using NewRelic.Agent.Core.Segments;
using NewRelic.Core.Logging;
using OtlpCollector = Opentelemetry.Proto.Collector.Trace.V1;
using OtlpCommon = Opentelemetry.Proto.Common.V1;
using OtlpResource = Opentelemetry.Proto.Resource.V1;
using OtlpTrace = Opentelemetry.Proto.Trace.V1;
using static Opentelemetry.Proto.Collector.Trace.V1.TraceService;

namespace NewRelic.Agent.Core.Aggregators
{
    public class SpanEventAggregatorOpenTelemetryProtocol : ISpanEventAggregatorInfiniteTracing
    {
        private readonly Channel channel;
        private readonly TraceServiceClient traceServiceClient;
        private readonly IConfigurationService _configurationService;

        public SpanEventAggregatorOpenTelemetryProtocol(IConfigurationService configurationService)
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

            channel = new Channel(endpoint.Authority, channelCredentials);
            traceServiceClient = new TraceServiceClient(channel);
            _configurationService = configurationService;
        }

        private Uri GetEndpoint()
        {
            var endpoint = System.Environment.GetEnvironmentVariable("NEW_RELIC_INFINITE_TRACING_OTLP_ENDPOINT");
            try
            {
                return new Uri(endpoint);
            }
            catch
            {
                Log.Error($"Invalid Uri configured for NEW_RELIC_INFINITE_TRACING_OTLP_ENDPOINT = {endpoint}. Defaulting to http://localhost:4317.");
                return new Uri("http://localhost:4317");
            }
        }

        public bool IsServiceEnabled => true;

        public bool IsServiceAvailable => true;

        public int Capacity => throw new NotImplementedException();

        public void Collect(ISpanEventWireModel wireModel)
        {
            throw new NotImplementedException();
        }

        public void Collect(IEnumerable<ISpanEventWireModel> wireModels)
        {
            try
            {
                var request = CreateExportTraceServiceRequest(wireModels);
                traceServiceClient.Export(request);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to export spans {ex}");
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

        private OtlpCollector.ExportTraceServiceRequest CreateExportTraceServiceRequest(IEnumerable<ISpanEventWireModel> wireModels)
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

        public bool HasCapacity(int proposedItems)
        {
            return true;
        }

        public void RecordDroppedSpans(int countDroppedSpans)
        {
            throw new NotImplementedException();
        }

        public void RecordSeenSpans(int countSeenSpans)
        {
            throw new NotImplementedException();
        }

        public void ReportSupportabilityMetrics()
        {
            throw new NotImplementedException();
        }
    }

    internal static class OtlpExtensions
    {
        public static void SetAttribute(this OtlpResource.Resource resource, string key, string value)
        {
            var attribute = new OtlpCommon.KeyValue { Key = key, Value = new OtlpCommon.AnyValue { } };
            attribute.Value.StringValue = value;
            resource.Attributes.Add(attribute);
        }

        public static OtlpTrace.Span ToOtlpSpan(this ISpanEventWireModel wireModel)
        {
            var span = wireModel.Span;

            var traceIdBytes = new byte[16];
            var spanIdBytes = new byte[8];

            ActivityTraceId.CreateFromString(span.Intrinsics["traceId"].StringValue.AsSpan()).CopyTo(traceIdBytes);
            ActivitySpanId.CreateFromString(span.Intrinsics["guid"].StringValue.AsSpan()).CopyTo(spanIdBytes);

            var parentSpanIdString = ByteString.Empty;
            if (span.Intrinsics.ContainsKey("parentId"))
            {
                var parentSpanIdBytes = new byte[8];
                ActivitySpanId.CreateFromString(span.Intrinsics["parentId"].StringValue.AsSpan()).CopyTo(parentSpanIdBytes);
                parentSpanIdString = ByteString.CopyFrom(parentSpanIdBytes);
            }

            var startTimeUnixNano = span.Intrinsics["timestamp"].IntValue * 1000000;
            long duration = TimeSpan.FromSeconds(span.Intrinsics["duration"].DoubleValue).Milliseconds;

            var spanKind = OtlpTrace.Span.Types.SpanKind.Internal;

            var categoryAttribute = span.Intrinsics["category"].StringValue;
            var isNrEntryPoint = span.Intrinsics.ContainsKey("nr.entryPoint");

            if (isNrEntryPoint && categoryAttribute == "generic")
            {
                spanKind = OtlpTrace.Span.Types.SpanKind.Server;
            }
            else if (categoryAttribute == "http")
            {
                spanKind = OtlpTrace.Span.Types.SpanKind.Client;
            }

            var otlpSpan = new OtlpTrace.Span
            {
                Name = span.Intrinsics["name"].StringValue,
                Kind = spanKind,

                TraceId = ByteString.CopyFrom(traceIdBytes),
                SpanId = ByteString.CopyFrom(spanIdBytes),
                ParentSpanId = parentSpanIdString,

                StartTimeUnixNano = (ulong)startTimeUnixNano,
                EndTimeUnixNano = (ulong)(startTimeUnixNano + duration * 1000000),
            };

            // TODO: Filter out trace, span, parent IDs
            foreach (var attribute in span.Intrinsics)
            {
                var otlpAttribute = attribute.ToOtlpAttribute();
                otlpSpan.Attributes.Add(otlpAttribute);
            }

            foreach (var attribute in span.AgentAttributes)
            {
                var otlpAttribute = attribute.ToOtlpAttribute();
                otlpSpan.Attributes.Add(otlpAttribute);
            }

            foreach (var attribute in span.UserAttributes)
            {
                var otlpAttribute = attribute.ToOtlpAttribute();
                otlpSpan.Attributes.Add(otlpAttribute);
            }

            var errorClass = span.AgentAttributes.ContainsKey("error.class")
                ? span.AgentAttributes["error.class"].StringValue
                : null;

            var errorMessage = span.AgentAttributes.ContainsKey("error.message")
                ? span.AgentAttributes["error.message"].StringValue
                : null;

            var errorExpected = span.AgentAttributes.ContainsKey("error.expected")
                ? span.AgentAttributes["error.expected"].BoolValue
                : false;

            var hasError = false;
            var errorDescription = string.Empty;
            if (!errorExpected && errorClass != null)
            {
                hasError = true;
                errorDescription = $"{errorClass}: {errorMessage}";
            }

            otlpSpan.Status = new OtlpTrace.Status
            {
                Code = !hasError
                    ? OtlpTrace.Status.Types.StatusCode.Unset
                    : OtlpTrace.Status.Types.StatusCode.Error
            };

            if (hasError)
            {
                otlpSpan.Status.Message = errorDescription;
            }

            return otlpSpan;
        }

        internal static OtlpCommon.KeyValue ToOtlpAttribute(this KeyValuePair<string, AttributeValue> kvp)
        {
            if (kvp.Value == null)
            {
                return null;
            }

            var attrib = new OtlpCommon.KeyValue { Key = kvp.Key, Value = new OtlpCommon.AnyValue { } };

            switch (kvp.Value.ValueCase)
            {
                case AttributeValue.ValueOneofCase.StringValue:
                    attrib.Value.StringValue = kvp.Value.StringValue;
                    break;
                case AttributeValue.ValueOneofCase.BoolValue:
                    attrib.Value.BoolValue = kvp.Value.BoolValue;
                    break;
                case AttributeValue.ValueOneofCase.IntValue:
                    attrib.Value.IntValue = kvp.Value.IntValue;
                    break;
                case AttributeValue.ValueOneofCase.DoubleValue:
                    attrib.Value.DoubleValue = kvp.Value.DoubleValue;
                    break;
                default:
                    return null;
            }

            return attrib;
        }
    }
}
