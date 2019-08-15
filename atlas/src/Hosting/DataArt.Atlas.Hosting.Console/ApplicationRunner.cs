#region License
// =================================================================================================
// Copyright 2018 DataArt, Inc.
// -------------------------------------------------------------------------------------------------
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this work except in compliance with the License.
// You may obtain a copy of the License in the LICENSE file, or at:
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// =================================================================================================
#endregion
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DataArt.Atlas.Hosting.Console
{
    public sealed class ApplicationRunner : IApplicationRunner
    {
        private const int UnableToLaunchApplicationError = 1;
        private static readonly AutoResetEvent Closing = new AutoResetEvent(false);

        public void Run(IApplication appInstance)
        {
            InitializeHostSpecificFunctions(appInstance);

            try
            {
                appInstance.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // write error output in case error happened before logger was initialized
                System.Console.WriteLine("Unhandled exception while starting console service");
                System.Console.WriteLine(ex.Message);

                Log.Fatal(ex, "Unhandled exception while starting console service");

                Environment.Exit(UnableToLaunchApplicationError);
            }

            System.Console.Title = $"{appInstance.GetType().Name}{GetConsoleTitle(appInstance.HostingSettings.Url)}";

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                }
            });

            System.Console.CancelKeyPress += OnExit;

            Closing.WaitOne();

            appInstance.StopAsync().GetAwaiter().GetResult();
        }

        private static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            System.Console.WriteLine("Exit");
            Closing.Set();
        }

        private static void InitializeHostSpecificFunctions<TService>(TService appInstance) where TService : IApplication
        {
            appInstance.ReportHealthStateAction = (property, state, description) => { };
            appInstance.ReportRecurrentHealthStateAction = (property, state, timeToLive, description) => { };
            appInstance.GetCodePackageVersionFunction =
                () => Assembly.GetEntryAssembly().GetName().Version.ToString();
            appInstance.GetDataPackageVersionFunction = packageName => $"{packageName}.{DateTime.Now.Ticks}";
        }

        private static string GetConsoleTitle(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            // most probably we receive not full url but http://+:<port>
            url = url.Replace("+", "localhost");
            var uriBuilder = new UriBuilder(url);
            return $"@{uriBuilder.Port} ({uriBuilder.Scheme})";
        }
    }
}
