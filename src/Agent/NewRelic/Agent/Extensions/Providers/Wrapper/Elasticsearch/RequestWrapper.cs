// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Parsing;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Reflection;

namespace NewRelic.Providers.Wrapper.Elasticsearch
{

    public class RequestWrapper : IWrapper
    {
        public bool IsTransactionRequired => true;

        private const string AssemblyName = "Elasticsearch.Net";

        private const string WrapperName = "RequestWrapper";


        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));

        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            //Elasticsearch.Net.HttpMethod,System.String,Elasticsearch.Net.PostData,Elasticsearch.Net.IRequestParameters
            var path = (string)instrumentedMethodCall.MethodCall.MethodArguments[1];
            var postData = instrumentedMethodCall.MethodCall.MethodArguments[2];
            var requestParams = instrumentedMethodCall.MethodCall.MethodArguments[3];

            var databaseName = string.Empty;
            var model = path.Split('/')[0];

            // TODO: get these somehow
            var operation = string.Empty;
            Uri endpoint = null;

            var segment = transaction.StartDatastoreSegment(
                instrumentedMethodCall.MethodCall,
                new ParsedSqlStatement(DatastoreVendor.Elasticsearch, model, operation),
                connectionInfo: endpoint != null ? new ConnectionInfo(endpoint.Host, endpoint.Port.ToString(), databaseName) : new ConnectionInfo(string.Empty, string.Empty, databaseName),
                isLeaf: true);

            return Delegates.GetDelegateFor(segment);
        }
    }
}
