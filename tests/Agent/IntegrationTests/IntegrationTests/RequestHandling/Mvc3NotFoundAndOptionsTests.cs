// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System.Net.Http;
using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Agent.IntegrationTests.RemoteServiceFixtures;
using Xunit;
using Xunit.Abstractions;

namespace NewRelic.Agent.IntegrationTests.RequestHandling
{
    [NetFrameworkTest]
    [Trait("TestAppType", "HWC")]
    public class Mvc3NotFoundAndOptionsTests : NotFoundAndOptionsTests<BasicMvcApplicationTestFixture>
    {
        public Mvc3NotFoundAndOptionsTests(BasicMvcApplicationTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output) { }

        protected override void ExerciseApplication()
        {
            _fixture.Get404("Default/MissingAction");
            _fixture.Get404("MissingController");
            _fixture.Request(HttpMethod.Options);
        }
    }
}
