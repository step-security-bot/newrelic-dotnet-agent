// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace HostedWebCore
{
    public class HostedWebCore
    {
        private const int ServerTimeoutShutdownMinutes = 5;

        private readonly string _port;
        private readonly bool _debug;

        private static string AssemblyDirectory
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Uri.LocalPath);
                return Path.GetDirectoryName(path);
            }
        }

        private static string ApplicationHostConfigFilePath
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return AssemblyDirectory + @"\applicationHost.config";
            }
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_port != null);
        }

        public HostedWebCore(string port, bool debug)
        {
            Contract.Requires(port != null);

            _port = port;
            _debug = debug;
        }

        public void Run()
        {
            try
            {
                StartWebServer();
            }
            catch (Exception ex)
            {
                Log($"HostedWebCore.Run().StartWebServer(): caught exception: {ex}");
                throw;
            }
            try
            {
                //The HWC creates this shutdown event and waits for the test runner to set so that it can shutdown.  
                var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, "app_server_wait_for_all_request_done_" + _port.ToString());
                CreatePidFile();
                eventWaitHandle.WaitOne(TimeSpan.FromMinutes(ServerTimeoutShutdownMinutes));
                FinishWebServer();
            }
            catch (Exception ex)
            {
                Log($"HostedWebCore.Run(): caught exception in rest of method: {ex}");
                throw;
            }
        }

        private void StartWebServer()
        {
            if (_debug)
            {
                Console.WriteLine("Pausing before activating webcore...");
                Console.ReadKey();
            }
            var hresult = NativeMethods.WebCoreActivate(ApplicationHostConfigFilePath, null, @".NET Agent Integration Test Web Host");
            if (_debug)
            {
                Console.WriteLine($"Pausing after activating webcore (HRESULT={hresult})...");
                Console.ReadKey();
            }
            Marshal.ThrowExceptionForHR(hresult);
        }

        private static void FinishWebServer()
        {
            var hresult = NativeMethods.WebCoreShutdown(true);
            if (hresult != 0)
                throw new Exception("Error occurred when calling WebCoreShutdown.  HResult: " + hresult);
        }

        private static void CreatePidFile()
        {
            var pid = Process.GetCurrentProcess().Id;
            var thisAssemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var pidFilePath = thisAssemblyPath + ".pid";
            File.WriteAllText(pidFilePath, pid.ToString(CultureInfo.InvariantCulture));
        }
        private static void Log(string format)
        {
            string prefix = string.Format("[{0} {1}-{2}] HostedWebCore: ", DateTime.Now,
                    System.Diagnostics.Process.GetCurrentProcess().Id,
                    System.Threading.Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine(prefix + format);
        }

    }
}
