// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Agent.Core.Segments;
using NewRelic.Reflection;
using System;

namespace NewRelic.Providers.Wrapper.GrpcNetClient
{
    public class FinishCallWrapper:IWrapper
    {
        Func<object, object> _getStatusFunc;
        Func<object, object> GetStatusFunc => _getStatusFunc ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<object>("Grpc.Core.Api", "Grpc.Core.Status", "StatusCode");

        public bool IsTransactionRequired => false;

        private const string WrapperName = "FinishCallWrapper";

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            var status = instrumentedMethodCall.MethodCall.MethodArguments[3];

            var statusCode = GetStatusFunc(status);

            var segment = transaction.CurrentSegment as Segment;

            var externalData = segment.Data as ExternalSegmentData;

            externalData.SetGrpcStatusCode((int)statusCode);

            segment.End();
            transaction.Release();

            return Delegates.NoOp;
        }
    }
}
