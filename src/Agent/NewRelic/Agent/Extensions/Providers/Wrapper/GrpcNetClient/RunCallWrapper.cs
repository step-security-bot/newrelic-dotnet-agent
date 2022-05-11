// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using NewRelic.Agent.Api;
using NewRelic.Agent.Api.Experimental;
using NewRelic.Agent.Extensions.Providers.Wrapper;

namespace NewRelic.Providers.Wrapper.AspNetCore.GrpcNetClient
{
    public class RunCallWrapper
    {
        public bool IsTransactionRequired => false;

        private const string WrapperName = "RunCallWrapper";

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            if (instrumentedMethodCall.IsAsync)
            {
                transaction.AttachToAsync();
            }

            var httpRequestMessage = instrumentedMethodCall.MethodCall.MethodArguments[0] as HttpRequestMessage;

            var method = (httpRequestMessage.Method != null ? httpRequestMessage.Method.Method : "<unknown>") ?? "<unknown>";

            var transactionExperimental = transaction.GetExperimentalApi();

            var externalSegmentData = transactionExperimental.CreateExternalSegmentData(httpRequestMessage.RequestUri, method);
            var segment = transactionExperimental.StartSegment(instrumentedMethodCall.MethodCall);
            segment.GetExperimentalApi().SetSegmentData(externalSegmentData);

            return Delegates.NoOp;
        }
    }
}
