// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System;
using System.Collections;
using System.Diagnostics.Contracts;
using CommandLine;
using System.IO;
using System.Text;
using System.Net.Sockets;

namespace HostedWebCore
{
    static class Program
    {
        private static void Log(string format)
        {
            string prefix = string.Format("[{0} {1}-{2}] HostedWebCore: ", DateTime.Now,
                    System.Diagnostics.Process.GetCurrentProcess().Id,
                    System.Threading.Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine(prefix + format);
        }

        private static void Main(string[] args)
        {
            string msg = "Firing up...args: " + string.Join(", ", args);
            Log("Firing up...args: " + string.Join(", ", args));
            Log("Starting directory: " + Directory.GetCurrentDirectory());

            var environmentVariables = new StringBuilder();
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                environmentVariables.Append($"  {de.Key} = {de.Value}; ");
            }
            Log("Environment Variables: " + environmentVariables.ToString());

            if (Parser.Default == null)
                throw new NullReferenceException("CommandLine.Parser.Default");

            var options = new Options();
            if (!Parser.Default.ParseArgumentsStrict(args, options))
                return;

            Contract.Assume(options.Port != null);

            var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
            var myPid = thisProcess.Id;

            var retries = 3;
            var retry = true;
            while (retry && retries-- > 0)
            {
                retry = false;
                try
                {
                    var hostedWebCore = new HostedWebCore(options.Port, options.Debug);
                    Log($"Starting server with pid {myPid}...");
                    hostedWebCore.Run();
                    Log("Done.");
                }
                catch (DllNotFoundException ex)
                {
                    Log($"HostedWebCore.exe failed: Check that the Hostable Web Core Windows feature is installed.: {ex}");
                    Environment.Exit(4);
                }
                catch (FileNotFoundException ex)
                {
                    Log($"HostedWebCore.exe failed: Running HostedWebCore.exe requires certain IIS components to be installed.: {ex}");
                    Environment.Exit(3);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log($"HostedWebCore.exe failed: This application must be run with administrator privileges.: {ex}");
                    Environment.Exit(2);
                }
                catch (FileLoadException ex)
                {
                    Log($"HostedWebCore.exe failed: FileLoadException.  Port {options.Port} is in use?");
                    CheckPortInUse(options.Port);
                    System.Threading.Thread.Sleep(1000); // wait a second and then retry
                    retry = true;
                }
                catch (Exception ex)
                {
                    Log($"HostedWebCore.exe failed: Reason unknown.: {ex}");
                    Environment.Exit(1);
                }
            }
        }

        private static void CheckPortInUse(string port)
        {
            Log($"Checking to see if port {port} is available...");
            try
            {
                var tcpListener = new TcpListener(System.Net.IPAddress.Any, int.Parse(port));
                tcpListener.ExclusiveAddressUse = true;
                tcpListener.Start();
                tcpListener.Stop();

                var tcp6Listener = new TcpListener(System.Net.IPAddress.IPv6Any, int.Parse(port));
                tcp6Listener.ExclusiveAddressUse = true;
                tcp6Listener.Start();
                tcp6Listener.Stop();

                Log($"Port {port} appears to be available.");
            }
            catch (Exception ex)
            {
                Log($"Port {port} appears to be in use, exception is: {ex}");
            }
        }
    }
}
