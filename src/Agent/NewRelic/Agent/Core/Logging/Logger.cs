// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using NewRelic.Agent.Core.AgentHealth;
using NewRelic.Agent.Core.DependencyInjection;
using NewRelic.Agent.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;

namespace NewRelic.Agent.Core.Logging
{
    public class Logger : ILogger, global::NewRelic.Core.Logging.ILogger
    {
        private readonly IAgentHealthReporter _agentHealthReporter;

        // not great...
        public Logger(IAgentHealthReporter agentHealthReporter = null)
        {
            try
            {
                if (agentHealthReporter != null)
                {
                    _agentHealthReporter = agentHealthReporter;
                    _agentHealthReporter.ReportLogForwardingFramework("AmagadLikeTotallyNewrelic");
                }
            }
            catch (Exception ex)
            {
                // TODO: just ignore
                Debugger.Launch();
                Debugger.Break();
                Console.WriteLine(ex.ToString());
            }

        }

        private readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(Logger));

        private void EnsureThreadIdPropertyExistsInContext()
        {
            log4net.ThreadContext.Properties["threadid"] ??= Thread.CurrentThread.ManagedThreadId;
        }

        public bool IsEnabledFor(Level level)
        {
            switch (level)
            {
                case Level.Finest:
                    return _logger.Logger.IsEnabledFor(log4net.Core.Level.Finest);
                case Level.Debug:
                    return _logger.IsDebugEnabled;
                case Level.Info:
                    return _logger.IsInfoEnabled;
                case Level.Warn:
                    return _logger.IsWarnEnabled;
                case Level.Error:
                    return _logger.IsErrorEnabled;
                default:
                    return false;
            }
        }

        public void Log(Level level, object message)
        {
            if (!IsEnabledFor(level))
            {
                return;
            }

            var messageString = message.ToString();

            switch (level)
            {
                case Level.Finest:
                    FinestInternal(message);
                    break;
                case Level.Debug:
                    DebugInternal(messageString);
                    break;
                case Level.Info:
                    InfoInternal(messageString);
                    break;
                case Level.Warn:
                    WarnInternal(messageString);
                    break;
                case Level.Error:
                    ErrorInternal(messageString);
                    break;
                default:
                    break;
            }
        }

        #region Error

        /// <summary>
        /// True iff logging has been configured to include ERROR level logs.
        /// </summary>
        public bool IsErrorEnabled => _logger.IsErrorEnabled;

        /// <summary>
        /// Logs <paramref name="message"/> at the ERROR level. This log level should be used for information regarding problems in the agent that will adversely affect the user in some way (data loss, performance problems, reduced agent functionality, etc). Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void Error(string message)
        {
            ErrorInternal(message);
        }

        /// <summary>
        /// Logs <paramref name="exception"/> at the ERROR level by calling exception.ToString(). This log level should be used for information regarding problems in the agent that will adversely affect the user in some way (data loss, performance problems, reduced agent functionality, etc). Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void Error(Exception exception)
        {
            ErrorInternal(exception.ToString());
        }

        /// <summary>
        /// Logs a message at the ERROR level by calling String.Format(<paramref name="format"/>, <paramref name="args"/>). This log level should be used for information regarding problems in the agent that will adversely affect the user in some way (data loss, performance problems, reduced agent functionality, etc). Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void ErrorFormat(string format, params object[] args)
        {
            if (IsErrorEnabled)
            {
                ErrorInternal(string.Format(format, args));
            }
        }

        private void ErrorInternal(object message)
        {
            EnsureThreadIdPropertyExistsInContext();
            _logger.Error(message);
            RecordLogSupportabilityMetric("Error");
        }

        #endregion Error

        #region Warn

        /// <summary>
        /// True iff logging has been configured to include WARN level logs.
        /// </summary>
        public bool IsWarnEnabled => _logger.IsWarnEnabled;

        /// <summary>
        /// Logs <paramref name="message"/> at the WARN level. This log level should be used for information regarding *possible* problems in the agent that *might* adversely affect the user in some way (data loss, performance problems, reduced agent functionality, etc). Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void Warn(string message)
        {
            WarnInternal(message);
        }

        /// <summary>
        /// Logs <paramref name="exception"/> at the WARN level by calling exception.ToString(). This log level should be used for information regarding *possible* problems in the agent that *might* adversely affect the user in some way (data loss, performance problems, reduced agent functionality, etc). Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void Warn(Exception exception)
        {
            WarnInternal(exception.ToString());
        }

        /// <summary>
        /// Logs a message at the WARN level by calling String.Format(<paramref name="format"/>, <paramref name="args"/>). This log level should be used for information regarding *possible* problems in the agent that *might* adversely affect the user in some way (data loss, performance problems, reduced agent functionality, etc). Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void WarnFormat(string format, params object[] args)
        {
            if (IsWarnEnabled)
            {
                WarnInternal(string.Format(format, args));
            }
        }

        private void WarnInternal(object message)
        {
            EnsureThreadIdPropertyExistsInContext();
            _logger.Warn(message);
            RecordLogSupportabilityMetric("Warn");
        }

        #endregion Warn

        #region Info

        /// <summary>
        /// True iff logging has been configured to include INFO level logs.
        /// </summary>
        public bool IsInfoEnabled => _logger.IsInfoEnabled;

        /// <summary>
        /// Logs <paramref name="message"/> at the INFO level. This log level should be used for information for non-error information that may be of interest to the user, such as a the agent noticing a configuration change, or an infrequent "heartbeat". Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void Info(string message)
        {
            InfoInternal(message);
        }

        /// <summary>
        /// Logs <paramref name="exception"/> at the INFO level by calling exception.ToString(). This log level should be used for information for non-error information that may be of interest to the user, such as a the agent noticing a configuration change, or an infrequent "heartbeat". Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void Info(Exception exception)
        {
            InfoInternal(exception.ToString());
        }

        /// <summary>
        /// Logs a message at the INFO level by calling String.Format(<paramref name="format"/>, <paramref name="args"/>). This log level should be used for information for non-error information that may be of interest to the user, such as a the agent noticing a configuration change, or an infrequent "heartbeat". Do not use if logging that information will create a performance problem (say, due to excessive logging).
        /// </summary>
        public void InfoFormat(string format, params object[] args)
        {
            if (IsInfoEnabled)
            {
                InfoInternal(string.Format(format, args));
            }
        }

        private void InfoInternal(object message)
        {
            EnsureThreadIdPropertyExistsInContext();
            _logger.Info(message);
            RecordLogSupportabilityMetric("Info");
        }

        #endregion Info

        #region Debug

        /// <summary>
        /// True iff logging has been configured to include DEBUG level logs.
        /// </summary>
        public bool IsDebugEnabled => _logger.IsDebugEnabled;

        /// <summary>
        /// Logs <paramref name="message"/> at the DEBUG level. This log level should be used for information that is non-critical and used mainly for troubleshooting common problems such as RUM injection or SQL explain plans. This level is not enabled by default so there is less concern about performance, but this level still should not be used for any logging that would cause significant performance, such as logging every transaction name for every transaction.
        /// </summary>
        public void Debug(string message)
        {
            DebugInternal(message);
        }

        /// <summary>
        /// Logs <paramref name="exception"/> at the DEBUG level by calling exception.ToString(). This log level should be used for information that is non-critical and used mainly for troubleshooting common problems such as RUM injection or SQL explain plans. This level is not enabled by default so there is less concern about performance, but this level still should not be used for any logging that would cause significant performance, such as logging every transaction name for every transaction.
        /// </summary>
        public void Debug(Exception exception)
        {
            DebugInternal(exception.ToString());
        }

        /// <summary>
        /// Logs a message at the DEBUG level by calling String.Format(<paramref name="format"/>, <paramref name="args"/>). This log level should be used for information that is non-critical and used mainly for troubleshooting common problems such as RUM injection or SQL explain plans. This level is not enabled by default so there is less concern about performance, but this level still should not be used for any logging that would cause significant performance, such as logging every transaction name for every transaction.
        /// </summary>
        public void DebugFormat(string format, params object[] args)
        {
            if (_logger.IsDebugEnabled)
            {
                DebugInternal(string.Format(format, args));
            }
        }

        private void DebugInternal(object message)
        {
            EnsureThreadIdPropertyExistsInContext();
            _logger.Debug(message);
            RecordLogSupportabilityMetric("Debug");
        }

        #endregion Debug

        #region Finest

        /// <summary>
        /// True iff logging has been configured to include FINEST level logs.
        /// </summary>
        public bool IsFinestEnabled => _logger.Logger.IsEnabledFor(log4net.Core.Level.Finest);

        /// <summary>
        /// Logs <paramref name="message"/> at the FINEST level. This log level should be used as a last resort for information that would otherwise be too expensive or too noisy to log at DEBUG level, such as logging every transaction name for every transaction. Useful for troubleshooting subtle problems like WCF's dual transactions.
        /// </summary>
        public void Finest(string message)
        {
            FinestInternal(message);
        }

        /// <summary>
        /// Logs <paramref name="exception"/> at the FINEST level by calling exception.ToString(). This log level should be used as a last resort for information that would otherwise be too expensive or too noisy to log at DEBUG level, such as logging every transaction name for every transaction. Useful for troubleshooting subtle problems like WCF's dual transactions.
        /// </summary>
        public void Finest(Exception exception)
        {
            FinestInternal(exception.ToString());
        }

        /// <summary>
        /// Logs a message at the FINEST level by calling String.Format(<paramref name="format"/>, <paramref name="args"/>). This log level should be used as a last resort for information that would otherwise be too expensive or too noisy to log at DEBUG level, such as logging every transaction name for every transaction. Useful for troubleshooting subtle problems like WCF's dual transactions.
        /// </summary>
        public void FinestFormat(string format, params object[] args)
        {
            if (IsFinestEnabled)
            {
                FinestInternal(string.Format(format, args));
            }
        }

        private void FinestInternal(object message)
        {
            EnsureThreadIdPropertyExistsInContext();
            _logger.Logger.Log(typeof(Logger), log4net.Core.Level.Finest, message, null);
            RecordLogSupportabilityMetric("Finest");
        }

        #endregion Finest

        private void RecordLogSupportabilityMetric(string level)
        {
            try
            {
                _agentHealthReporter?.ReportSupportabilityCountMetric($"Josh/InternalLogCount/{level}");
            }
            catch (Exception ex)
            {
                // TODO: just ignore
                Debugger.Launch();
                Debugger.Break();
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

