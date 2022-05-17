// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;

namespace NewRelic.Providers.Wrapper.GrpcAspNetCoreServer
{
    public class HandleCallAsyncWrapper : IWrapper
    {
        public bool IsTransactionRequired => true;

        private const string WrapperName = "HandleCallAsyncWrapper";

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {

            var httpContext = instrumentedMethodCall.MethodCall.MethodArguments[0] as HttpContext;

            var methodName = httpContext.Request.Path.Value.TrimStart('/');

            var segment = transaction.StartMethodSegment(instrumentedMethodCall.MethodCall, "gRPC", methodName);

            return Delegates.GetAsyncDelegateFor<Task>(agent, segment, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
