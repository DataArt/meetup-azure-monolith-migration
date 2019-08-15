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
using System.Threading;
using System.Threading.Tasks;
using DataArt.Atlas.Configuration.Settings;
using DataArt.Atlas.Hosting.HealthCheck;

namespace DataArt.Atlas.Hosting
{
    public interface IApplication
    {
        HostingSettings HostingSettings { get; }

        Task StartAsync(string applicationUrlOverride = null, CancellationToken cancellationToken = default(CancellationToken));

        Task StopAsync();

        Action<string, HealthState, string> ReportHealthStateAction { set; }

        Action<string, HealthState, TimeSpan, string> ReportRecurrentHealthStateAction { set; }

        Func<string> GetCodePackageVersionFunction { set; }

        Func<string, string> GetDataPackageVersionFunction { set; }

        Func<string, string> GetServiceResourcePathFunction { set; }
    }
}
