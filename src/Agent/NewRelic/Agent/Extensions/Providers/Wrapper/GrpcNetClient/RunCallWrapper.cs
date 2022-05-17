// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Net.Http;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;

namespace NewRelic.Providers.Wrapper.GrpcNetClient
{
    public class RunCallWrapper: IWrapper
    {
        public bool IsTransactionRequired => true;

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

            var method = httpRequestMessage.RequestUri.PathAndQuery.Split('?')[0].Trim('/');

            var port = httpRequestMessage.RequestUri.Port != -1 ? $":{httpRequestMessage.RequestUri.Port}" : string.Empty;

            var url = new Uri("grpc://" + httpRequestMessage.RequestUri.Host + port + "/" + method);
            
            transaction.StartExternalRequestSegment(instrumentedMethodCall.MethodCall, url, method, isLeaf: true);

            transaction.Hold();

            TryAttachHeadersToRequest(agent, httpRequestMessage);

            return Delegates.NoOp;
        }

        private static void TryAttachHeadersToRequest(IAgent agent, HttpRequestMessage httpRequestMessage)
        {
            var setHeaders = new Action<HttpRequestMessage, string, string>((carrier, key, value) =>
            {
                // "Add" will throw if value exists, so we must remove it first
                carrier.Headers?.Remove(key);
                carrier.Headers?.Add(key, value);
            });

            try
            {
                agent.CurrentTransaction.InsertDistributedTraceHeaders(httpRequestMessage, setHeaders);
            }
            catch (Exception ex)
            {
                agent.HandleWrapperException(ex);
            }
        }
    }
}
