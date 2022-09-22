// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using NewRelic.Agent.Api;

namespace NewRelic.Agent.Extensions.Helpers
{
    public interface IStackExchangeRedisCache : IDisposable
    {
        void Harvest(string spanId, Agent.Api.ITransaction transaction);
    }
}
