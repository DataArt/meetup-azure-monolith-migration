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
using DataArt.Atlas.Infrastructure.Exceptions;
using DataArt.Atlas.Infrastructure.Startup;
using DataArt.Atlas.Service.Scheduler.Sdk;
using DataArt.Atlas.Service.Scheduler.Sdk.Models;
using Quartz;
using Serilog;

namespace DataArt.Atlas.Service.Scheduler.Scheduler
{
    internal sealed class SchedulerService : Startable, IDisposable, ISchedulerService
    {
        private readonly ManualResetEvent invalidatedEvent = new ManualResetEvent(false);
        private readonly JobLockProvider jobLockProvider = new JobLockProvider();

        private readonly IAsyncScheduler scheduler;

        public SchedulerService(IAsyncScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        protected override async Task StartInternalAsync(CancellationToken cancellationToken)
        {
            await InvalidateJobsAsync(cancellationToken);
            await scheduler.StartAsync(cancellationToken);
        }

        protected override async Task StopInternalAsync()
        {
            await scheduler.ShutdownAsync();
        }

        public void Dispose()
        {
            scheduler?.ShutdownAsync().GetAwaiter().GetResult();
            jobLockProvider.Dispose();
            invalidatedEvent.Dispose();
        }

        public async Task ScheduleJobAsync(string jobId, JobDataModel jobData)
        {
            if (!SchedulerClientConfig.Version.Equals(jobData.SdkVersion))
            {
                throw new ApiValidationException("Invalid SDK version");
            }

            invalidatedEvent.WaitOne();

            var jobKey = JobKey.Create(jobId);

            using (await jobLockProvider.AcquireAsync(jobKey))
            {
                if (await scheduler.CheckJobExistsAsync(jobKey))
                {
                    var existingJobDetail = await scheduler.GetJobDetailAsync(jobKey);

                    if (jobData.GetDataHashCode().Equals(existingJobDetail.GetDataHashCode()))
                    {
                        Log.Debug("{jobId} job is up to date", jobId);
                        return;
                    }

                    Log.Debug("{jobId} job will be updated", jobId);

                    await scheduler.DeleteJobAsync(jobKey);
                    Log.Information("{jobId} job was deleted", jobKey.Name);
                }

                var newJobDetail = JobDetailFactory.Create(jobKey, jobData);
                await scheduler.CreateJobAsync(newJobDetail);
                Log.Information("{jobId} job was created", jobId);

                var newTrigger = TriggerFactory.Create(jobKey, jobData);
                var firstFireMoment = await scheduler.CreateTriggerAsync(newTrigger);
                Log.Information($"{{jobId}} job was scheduled (next execution: {firstFireMoment})", jobId);
            }
        }

        public async Task DeleteJobAsync(string jobId)
        {
            invalidatedEvent.WaitOne();

            var jobKey = JobKey.Create(jobId);

            using (await jobLockProvider.AcquireAsync(jobKey))
            {
                if (!await scheduler.CheckJobExistsAsync(jobKey))
                {
                    throw new NotFoundException(jobId);
                }

                await scheduler.DeleteJobAsync(jobKey);
            }
        }

        private async Task InvalidateJobsAsync(CancellationToken cancellationToken)
        {
            Log.Debug("Jobs invalidation started");

            foreach (var jobKey in await scheduler.GetJobKeysAsync())
            {
                try
                {
                    if (await CheckJobShouldBeInvalidatedAsync(jobKey))
                    {
                        await scheduler.DeleteJobAsync(jobKey);
                        Log.Information("{jobId} job was invalidted", jobKey.Name);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unable to invalidate {jobId} job", jobKey.Name);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            invalidatedEvent.Set();

            Log.Information("Jobs invalidation completed");
        }

        private async Task<bool> CheckJobShouldBeInvalidatedAsync(JobKey jobKey)
        {
            IJobDetail jobDetail;

            try
            {
                jobDetail = await scheduler.GetJobDetailAsync(jobKey);
            }
            catch (JobPersistenceException e)
            {
                Log.Warning(e, "{jobId} job has unsupported type and therefore it should be invalidated", jobKey.Name);
                return true;
            }

            var jobSdkVersion = jobDetail.GetSdkVersion();

            if (string.IsNullOrWhiteSpace(jobSdkVersion))
            {
                Log.Warning("{jobId} job was scheduled with deprecated version of scheduler sdk and therefore it should be invalidated", jobKey.Name);
                return true;
            }

            if (!SchedulerClientConfig.Version.Equals(jobSdkVersion))
            {
                Log.Warning($"{{jobId}} job was scheduled with {jobSdkVersion} version of scheduler sdk and therefore it should be invalidated (actual sdk version: {SchedulerClientConfig.Version})", jobKey.Name);
                return true;
            }

            return false;
        }
    }
}
