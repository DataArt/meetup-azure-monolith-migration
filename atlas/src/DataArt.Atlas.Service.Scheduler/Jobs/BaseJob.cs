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
using System.Threading.Tasks;
using DataArt.Atlas.CallContext.Correlation;
using DataArt.Atlas.Service.Scheduler.Scheduler;
using DataArt.Atlas.Service.Scheduler.Sdk;
using Quartz;
using Serilog;

namespace DataArt.Atlas.Service.Scheduler.Jobs
{
    public abstract class BaseJob<TJobSettings> : IJob where TJobSettings : class
    {
        public abstract Task ExecuteAsync(TJobSettings settings);

        public async Task Execute(IJobExecutionContext context)
        {
            var jobDetail = context.JobDetail;

            using (new CorrelatedSequence())
            {
                Log.Debug($"Starting {{jobId}} job (sdk version: {SchedulerClientConfig.Version}, scheduled with sdk version {jobDetail.GetSdkVersion()})", jobDetail.Key.Name);

                try
                {
                    var jobSettings = context.JobDetail.GetSettings<TJobSettings>();

                    await ExecuteAsync(jobSettings);

                    Log.Information($"{{jobId}} job executed (sdk version: {SchedulerClientConfig.Version}, scheduled with sdk version {jobDetail.GetSdkVersion()})", jobDetail.Key.Name);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"{{jobId}} job execution failed (sdk version: {SchedulerClientConfig.Version}, scheduled with sdk version {jobDetail.GetSdkVersion()})", jobDetail.Key?.Name);
                }
            }
        }
    }
}
