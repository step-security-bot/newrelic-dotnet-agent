// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Threading;
using NewRelic.Agent.Api;
using NewRelic.Agent.Api.Experimental;
using NewRelic.Agent.Extensions.Helpers;
using NewRelic.Agent.Extensions.Parsing;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;

namespace NewRelic.Providers.Wrapper.StackExchangeRedis
{
    public class SessionCache : IStackExchangeRedisCache
    {
        private readonly EventWaitHandle _stopHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        internal readonly ConcurrentDictionary<string, ProfilingSession> Cache = new ConcurrentDictionary<string, ProfilingSession>();

        private readonly ProfilingSession _defaultSession = new ProfilingSession();

        private readonly IAgent _agent;

        private readonly ConnectionInfo _connectionInfo;

        private readonly MethodCall _methodCall;

        public SessionCache(IAgent agent, ConnectionInfo connectionInfo, MethodCall methodCall)
        {
            _agent = agent;
            _connectionInfo = connectionInfo;
            _methodCall = methodCall;
        }

        public void Harvest(string spanId, Agent.Api.ITransaction transaction)
        {
            // the segment is finishing, we pull its session and call the api to make more segments
            // these new ones would be children of the existing segment.
            var xTransaction = (ITransactionExperimental)transaction;
            if (!Cache.TryRemove(spanId, out var session))
            {
                return;
            }

            var commands = session.FinishProfiling();
            _agent.Logger.Log(Agent.Extensions.Logging.Level.Info, "Harvest:" + spanId + ":commands=" + commands.Count());

            var startTime = xTransaction.StartTime;
            foreach (var command in commands)
            {
                var relativeStartTime = command.CommandCreated - startTime;
                var relativeEndTime = relativeStartTime + command.ElapsedTime;
                var operation = command.Command;

                // This new segment maker accepts relative start and stop times since we will be starting and ending the segment immediately.
                var segment = xTransaction.StartStackExchangeRedisSegment(_methodCall, ParsedSqlStatement.FromOperation(DatastoreVendor.Redis, operation),
                    _connectionInfo, relativeStartTime, relativeEndTime);

                // We can't call end since we set the times, but we still want to cleanup the callstack.
                segment.RemoveSegmentFromCallStack();
            }
        }
        public Func<ProfilingSession> GetProfilingSession()
        {
            return () =>
            {
                if (_stopHandle.WaitOne(0))
                {
                    return null;
                }

                var transaction = _agent.CurrentTransaction;
                if (!transaction.IsValid)
                {
                    return _defaultSession;
                }

                var segment = transaction.CurrentSegment;
                var spanId = segment.SpanId;
                if (!Cache.TryGetValue(spanId, out var session))
                {
                    session = new ProfilingSession(segment);
                    Cache.TryAdd(spanId, session);
                }

                return session;
            };
        }

        public void Dispose()
        {
            this._stopHandle.Set();
            this._stopHandle.Dispose();
        }
    }
}
