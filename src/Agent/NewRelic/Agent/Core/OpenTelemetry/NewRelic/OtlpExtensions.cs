// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.Protobuf;
using NewRelic.Agent.Core.Segments;
using OtlpCommon = Opentelemetry.Proto.Common.V1;
using OtlpResource = Opentelemetry.Proto.Resource.V1;
using OtlpTrace = Opentelemetry.Proto.Trace.V1;

namespace OpenTelemetry.NewRelic
{
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
