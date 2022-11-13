﻿// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using NewRelic.Agent.Api;
using NewRelic.Agent.Api.Experimental;
using NewRelic.Agent.Extensions.Logging;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Reflection;

namespace NewRelic.Providers.Wrapper.NLogLogging
{
    public class NLogWrapper : IWrapper
    {
        private static Func<object, object> _getLevel;
        private static Func<object, string> _getRenderedMessage;
        private static Func<object, DateTime> _getTimestamp;
        private static Func<object, string> _messageGetter;
        private static Func<object, Exception> _getLogException;
        private static Func<object, Dictionary<object, object>> _getPropertiesDictionary;

        public bool IsTransactionRequired => false;

        private const string WrapperName = "nlog";

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            // Since NLog can alter the messages directly, we need to move the MEL check
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            var logEvent = instrumentedMethodCall.MethodCall.MethodArguments[2];
            var logEventType = logEvent.GetType();

            if (!LogProviders.RegisteredLogProvider[(int)LogProvider.NLog])
            {
                RecordLogMessage(logEvent, logEventType, agent);
            }

            // We want this to happen instead of MEL so no provider check here.
            DecorateLogMessage(logEvent, logEventType, agent);

            return Delegates.NoOp;
        }

        private void RecordLogMessage(object logEvent, Type logEventType, IAgent agent)
        {
            var getLevelFunc = _getLevel ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<object>(logEventType, "Level");

            var getRenderedMessageFunc = _getRenderedMessage ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<string>(logEventType, "FormattedMessage");

            var getTimestampFunc = _getTimestamp ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<DateTime>(logEventType, "TimeStamp");

            var getLogExceptionFunc = _getLogException ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<Exception>(logEventType, "Exception");

            var getPropertiesDictionaryFunc = _getPropertiesDictionary ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<Dictionary<object,object>>(logEventType, "Properties");

            // This will either add the log message to the transaction or directly to the aggregator
            var xapi = agent.GetExperimentalApi();
            xapi.RecordLogMessage(WrapperName, logEvent, getTimestampFunc, getLevelFunc, getRenderedMessageFunc, getLogExceptionFunc, GetContextData, agent.TraceMetadata.SpanId, agent.TraceMetadata.TraceId);
        }

        private void DecorateLogMessage(object logEvent, Type logEventType, IAgent agent)
        {
            if (!agent.Configuration.LogDecoratorEnabled)
            {
                return;
            }

            var formattedMetadata = LoggingHelpers.GetFormattedLinkingMetadata(agent);

            var messageGetter = _messageGetter ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<string>(logEventType, "Message");

            // Message should not be null, but better to be sure
            var originalMessage = messageGetter(logEvent);
            if (string.IsNullOrWhiteSpace(originalMessage))
            {
                return;
            }

            // this cannot be made a static since it is unique to each logEvent
            var messageSetter = VisibilityBypasser.Instance.GeneratePropertySetter<string>(logEvent, "Message");
            messageSetter(originalMessage + " " + formattedMetadata);
        }

        private Dictionary<string,object> GetContextData(object logEvent)
        {
            var contextData = new Dictionary<string, object>();

            var properties = _getPropertiesDictionary(logEvent);
            foreach (var property in properties)
            {
                if (property.Key is string keyName)
                {
                    contextData[keyName] = property.Value;
                }
                else if (property.Key is int keyNum)
                {
                    contextData[keyNum.ToString()] = property.Value;
                }
                else
                {
                    contextData[property.Key.ToString()] = property.Value;
                }
            }

            return contextData;
        }
    }
}
