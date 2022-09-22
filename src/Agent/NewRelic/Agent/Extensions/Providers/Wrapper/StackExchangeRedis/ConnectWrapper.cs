// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using NewRelic.Agent.Api;
using NewRelic.Agent.Api.Experimental;
using NewRelic.Agent.Extensions.Parsing;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Parsing.ConnectionString;
using StackExchange.Redis;

namespace NewRelic.Providers.Wrapper.StackExchangeRedis
{
    public class ConnectWrapper : IWrapper
    {
        public bool IsTransactionRequired => true;

        private const string WrapperName = "testing-inst";

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, Agent.Api.ITransaction transaction)
        {
            var multiplexer = instrumentedMethodCall.MethodCall.MethodArguments[0] as IConnectionMultiplexer;

            // We need this information to create a DataStoreSegemnt  - using a new method called StartStackExchangeRedisSegment
            var connection = Common.GetConnectionInfoFromConnectionMultiplexer(multiplexer, agent.Configuration.UtilizationHostName);
            var method = new Method(typeof(IConnectionMultiplexer), "Execute", "");
            var methodCall = new MethodCall(method, multiplexer, new object[0]);


            var sessionCache = new SessionCache(agent, connection, methodCall);
            ((IAgentExperimental)agent).StackExchangeRedisCache = sessionCache;
            multiplexer.RegisterProfiler(sessionCache.GetProfilingSession());

            // This instrumentation likely runs only 1 since instruments the connection attempt.  We don't want a segment.
            return Delegates.NoOp;
        }
    }
}
