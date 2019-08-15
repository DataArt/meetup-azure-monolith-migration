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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataArt.Atlas.Infrastructure.Exceptions;
using DataArt.Atlas.Infrastructure.Startup;
using DataArt.Atlas.Service.Scheduler.Sdk.Models;
using DataArt.Atlas.WebCommunication.Exceptions;
using Polly;
using Serilog;

namespace DataArt.Atlas.Service.Scheduler.Sdk.JobRegistration
{
    public abstract class JobRegistrar<TScheduleSettings> : Startable, IJobRegistrar
    {
        private readonly ISchedulerClient schedulerClient;
        private readonly ScheduleType scheduleType;

        private CancellationTokenSource registrationTaskCancellationTokenSource;

        protected JobRegistrar(ScheduleType scheduleType, ISchedulerClient schedulerClient)
        {
            this.schedulerClient = schedulerClient;
            this.scheduleType = scheduleType;
        }

        protected abstract string JobId { get; }

        protected virtual bool IsJobEnabled => true;

        #region Web request job settings

        protected abstract string ServiceKey { get; }

        protected abstract string WebRequestUri { get; }

        protected virtual IDictionary<string, string> WebRequestParameters { get; } = null;

        protected virtual TimeSpan WebRequestTimeout { get; } = TimeSpan.FromMinutes(2);

        private WebRequestJobSettingsModel GetJobSettings()
        {
            return new WebRequestJobSettingsModel
            {
                ServiceKey = ServiceKey,
                Uri = WebRequestUri,
                Parameters = WebRequestParameters,
                Timeout = WebRequestTimeout
            };
        }

        #endregion

        protected abstract TScheduleSettings GetScheduleSettings();

        protected sealed override void StartInternal(CancellationToken cancellationToken)
        {
            if (!IsJobEnabled)
            {
                Log.Debug("{jobId} job registration is disabled", JobId);
                return;
            }

            registrationTaskCancellationTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                return Policy.Handle<Exception>()
                    .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (exception, timespan) => HandleRegisterJobException(exception, timespan))
                    .ExecuteAsync(RegisterJobAsync);
            }, registrationTaskCancellationTokenSource.Token);
        }

        protected override void StopInternal()
        {
            registrationTaskCancellationTokenSource.Cancel();
        }

        private Task RegisterJobAsync()
        {
            return schedulerClient.ScheduleJobAsync(JobId, new JobDataModel
            {
                SdkVersion = SchedulerClientConfig.Version,
                Settings = JobDataModel.SerializeSettings(GetJobSettings()),
                ScheduleType = scheduleType,
                ScheduleSettings = JobDataModel.SerializeSettings(GetScheduleSettings())
            });
        }

        private void HandleRegisterJobException(Exception exception, TimeSpan reTryTimeSpan)
        {
            if (exception is CommunicationException || exception is OperationCanceledException || exception is ApiValidationException)
            {
                Log.Warning(exception, $"Failed to register {{jobId}} job (sdk version: {SchedulerClientConfig.Version}, next try in {reTryTimeSpan})", JobId);
            }
            else
            {
                Log.Error(exception, $"Failed to register {{jobId}} job (sdk version: {SchedulerClientConfig.Version}), next try in {reTryTimeSpan}", JobId);
            }
        }
    }
}
