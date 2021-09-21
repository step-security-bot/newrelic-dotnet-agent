// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using NewRelic.Agent.Configuration;
using NewRelic.Agent.Core.Segments;
using OpenTelemetry.NewRelic;

namespace NewRelic.Agent.Core.Aggregators
{
    public class SpanEventAggregatorOpenTelemetryProtocol : ISpanEventAggregatorInfiniteTracing
    {
        private readonly BatchSpanExportProcessor batchExportProcessor;

        public SpanEventAggregatorOpenTelemetryProtocol(IConfigurationService configurationService)
        {
            batchExportProcessor = new BatchSpanExportProcessor(new OtlpSpanExporter(configurationService));
        }

        public bool IsServiceEnabled => true;

        public bool IsServiceAvailable => true;

        public int Capacity => throw new NotImplementedException();

        public void Collect(ISpanEventWireModel wireModel)
        {
            batchExportProcessor.OnEnd(wireModel);
        }

        public void Collect(IEnumerable<ISpanEventWireModel> wireModels)
        {
            foreach (var item in wireModels)
            {
                Collect(item);
            }
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
}
