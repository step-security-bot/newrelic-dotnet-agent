// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using NewRelic.Agent.Core.Segments;

namespace OpenTelemetry.NewRelic
{
    public class BatchSpanExportProcessor : BatchExportProcessor<ISpanEventWireModel>
    {
        public BatchSpanExportProcessor(
            BaseExporter<ISpanEventWireModel> exporter,
            int maxQueueSize = DefaultMaxQueueSize,
            int scheduledDelayMilliseconds = DefaultScheduledDelayMilliseconds,
            int exporterTimeoutMilliseconds = DefaultExporterTimeoutMilliseconds,
            int maxExportBatchSize = DefaultMaxExportBatchSize)
            : base(
                exporter,
                maxQueueSize,
                scheduledDelayMilliseconds,
                exporterTimeoutMilliseconds,
                maxExportBatchSize)
        {
        }
    }
}
