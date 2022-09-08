// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using MultiFunctionApplicationHelpers;
using System.Collections.Generic;
using System.Linq;
using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Testing.Assertions;
using Xunit;
using Xunit.Abstractions;
using NewRelic.Agent.IntegrationTestHelpers.Models;

namespace NewRelic.Agent.IntegrationTests.CustomInstrumentation
{
    [NetFrameworkTest]
    public class CustomSpanNameApiTestsFW462 : CustomSpanNameApiTests<ConsoleDynamicMethodFixtureFW462>
    {
        public CustomSpanNameApiTestsFW462(ConsoleDynamicMethodFixtureFW462 fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }

    [NetCoreTest]
    public class CustomSpanNameApiTestsCore31 : CustomSpanNameApiTests<ConsoleDynamicMethodFixtureCore31>
    {
        public CustomSpanNameApiTestsCore31(ConsoleDynamicMethodFixtureCore31 fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }

    public abstract class CustomSpanNameApiTests<TFixture> : NewRelicIntegrationTest<TFixture> where TFixture : ConsoleDynamicMethodFixture
    {
        protected readonly TFixture Fixture;

        public CustomSpanNameApiTests(TFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            Fixture = fixture;
            Fixture.TestLogger = output;

            Fixture.AddCommand($"AttributeInstrumentation TransactionWithCustomSpanName CustomSpanName");
            Fixture.AddCommand("RootCommands DelaySeconds 5");

            Fixture.Actions
            (
                setupConfiguration: () =>
                {
                    var configPath = fixture.DestinationNewRelicConfigFilePath;
                    var configModifier = new NewRelicConfigModifier(configPath);
                    configModifier.ForceTransactionTraces();
                }
            );

            Fixture.Initialize();
        }

        [Fact]
        public void MetricsHaveCustomSpanName()
        {
            var expectedMetrics = new List<Assertions.ExpectedMetric>
            {
                new Assertions.ExpectedMetric { metricName = $"DotNet/CustomSpanName", callCount = 1 },
                new Assertions.ExpectedMetric { metricName = $"DotNet/CustomSpanName", metricScope = "OtherTransaction/Custom/MultiFunctionApplicationHelpers.NetStandardLibraries.Internal.AttributeInstrumentation/TransactionWithCustomSpanName", callCount = 1 }
            };

            var metrics = Fixture.AgentLog.GetMetrics().ToList();

            Assertions.MetricsExist(expectedMetrics, metrics);
        }

        [Fact]
        public void TransactionTraceContainsSegmentWithCustomSpanName()
        {
            var transactionTrace = Fixture.AgentLog.GetTransactionSamples().FirstOrDefault();
            Assert.NotNull(transactionTrace);

            transactionTrace.TraceData.ContainsSegment("CustomSpanName");
        }

        [Fact]
        public void SpanEventDataHasCustomSpanName()
        {
            var spanEvents = Fixture.AgentLog.GetSpanEvents();
            Assert.Contains(spanEvents, x => (string)x.IntrinsicAttributes["name"] == "CustomSpanName");
        }
    }
}
