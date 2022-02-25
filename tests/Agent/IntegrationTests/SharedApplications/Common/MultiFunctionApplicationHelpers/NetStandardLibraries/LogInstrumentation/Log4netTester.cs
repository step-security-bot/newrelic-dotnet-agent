﻿// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using NewRelic.Agent.IntegrationTests.Shared.ReflectionHelpers;
using NewRelic.Api.Agent;

namespace MultiFunctionApplicationHelpers.NetStandardLibraries.LogInstrumentation
{
    [Library]
    public static class Log4netTester
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Log4netTester));

        [LibraryMethod]
        public static void Configure()
        {
            BasicConfigurator.Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()));
        }


        [LibraryMethod]
        public static void ConfigurePatternLayoutAppenderForDecoration()
        {
            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%timestamp [%thread] %level %logger %ndc - %message %property{NR_LINKING}%newline";
            patternLayout.ActivateOptions();

            ConsoleAppender consoleAppender = new ConsoleAppender();
            consoleAppender.Layout = patternLayout;
            consoleAppender.ActivateOptions();

            BasicConfigurator.Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()), consoleAppender);
        }

#if LOG4NET_JSON_FORMATTER_SUPPORTED
        [LibraryMethod]
        public static void ConfigureJsonLayoutAppenderForDecoration()
        {
            SerializedLayout serializedLayout = new SerializedLayout();
            serializedLayout.AddMember("NR_LINKING");
            serializedLayout.ActivateOptions();

            ConsoleAppender consoleAppender = new ConsoleAppender();
            consoleAppender.Layout = serializedLayout;
            consoleAppender.ActivateOptions();

            BasicConfigurator.Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()), consoleAppender);
        }
#endif

        [LibraryMethod]
        public static void CreateSingleLogMessage(string message, string level)
        {
            switch (level.ToUpper())
            {
                case "DEBUG":
                    log.Debug(message);
                    break;
                case "INFO":
                    log.Info(message);
                    break;
                case "WARN":
                case "WARNING":
                    log.Warn(message);
                    break;
                case "ERROR":
                    log.Error(message);
                    break;
                case "FATAL":
                    log.Fatal(message);
                    break;
                default:
                    log.Info(message);
                    break;
            }
        }

        [LibraryMethod]
        [Transaction]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static void CreateSingleLogMessageInTransaction(string message, string level)
        {
            CreateSingleLogMessage(message, level);
        }

        [LibraryMethod]
        public static async Task CreateSingleLogMessageAsync(string message, string level)
        {
            await Task.Run(() => CreateSingleLogMessage(message, level));
        }

        [LibraryMethod]
        [Transaction]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static async Task CreateSingleLogMessageInTransactionAsync(string message, string level)
        {
            await Task.Run(() => CreateSingleLogMessage(message, level));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        [LibraryMethod]
        public static async Task CreateSingleLogMessageAsyncNoAwait(string message, string level)
        {
            _ = Task.Run(() => CreateSingleLogMessage(message, level));
        }

        [LibraryMethod]
        [Transaction]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]

        public static async Task CreateSingleLogMessageInTransactionAsyncNoAwait(string message, string level)
        {
            _ = Task.Run(() => CreateSingleLogMessage(message, level));
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        [LibraryMethod]
        [Transaction]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static async Task CreateSingleLogMessageInTransactionAsyncNoAwaitWithDelay(string message, string level)
        {
            _ = Task.Run(() => CreateSingleLogMessage(message, level));
            await Task.Delay(1000);
        }

        [LibraryMethod]
        [Trace]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static void CreateSingleLogMessageWithTraceAttribute(string message, string level)
        {
            CreateSingleLogMessage(message, level);
        }

        [LibraryMethod]
        [Transaction]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static void CreateTwoLogMessagesInTransactionWithDifferentTraceAttributes(string message, string level)
        {
            CreateSingleLogMessage(message, level);
            CreateSingleLogMessageWithTraceAttribute(message, level);
        }
    }
}