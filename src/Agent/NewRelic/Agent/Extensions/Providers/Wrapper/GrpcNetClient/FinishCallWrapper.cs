// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Agent.Core.Segments;
using System.Reflection;

namespace NewRelic.Providers.Wrapper.GrpcNetClient
{
    /// <summary>
    /// This wrapper is used to capture grpc request status code.
    /// </summary>
    public class FinishCallWrapper:IWrapper
    {
        //Theoratically, this wrapper does require a transaction; however, if IsTransactionRequired is set to true, this wrapper
        //won't get called. This is not because no transaction, but due to the RunCallWrapper created a leaf segment (External segment) which prevents
        //any nested intrumentation to occur.
        public bool IsTransactionRequired => false;

        private static PropertyInfo _statusCodeProperty;

        private const string WrapperName = "FinishCallWrapper";

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            var status = instrumentedMethodCall.MethodCall.MethodArguments[3];

            if(_statusCodeProperty == null)
                _statusCodeProperty = status.GetType().GetProperty("StatusCode");

            var statusCode = _statusCodeProperty.GetValue(status);

            var segment = transaction.CurrentSegment as Segment;

            var externalData = segment.Data as ExternalSegmentData;

            externalData.SetGrpcStatusCode((int)statusCode);

            segment.End();
            transaction.Release();

            return Delegates.NoOp;
        }
    }
}
