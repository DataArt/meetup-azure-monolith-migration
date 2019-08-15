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
using DataArt.Atlas.CallContext.Correlation;
using DataArt.Atlas.Hosting.HealthCheck;
using DataArt.Atlas.Infrastructure.Startup;
using Serilog;

namespace DataArt.Atlas.Service.HealthCheck
{
    public abstract class RecurrentHealthCheck : Startable, IDisposable
    {
        private readonly TimeSpan recurrenceInterval;
        private readonly IHealthReporter healthReporter;

        private readonly TimeSpan timeToLivePadding = TimeSpan.FromSeconds(30);

        private Timer timer;

        protected RecurrentHealthCheck(TimeSpan recurrenceInterval, IHealthReporter healthReporter)
        {
            this.recurrenceInterval = recurrenceInterval;
            this.healthReporter = healthReporter;
        }

        protected abstract string Property { get; }

        protected abstract HealthState HealthCheck();

        protected override void StartInternal(CancellationToken cancellationToken)
        {
            timer = new Timer(state => HealthCheckInternal(), null, TimeSpan.Zero, recurrenceInterval);
        }

        private void HealthCheckInternal()
        {
            using (new CorrelatedSequence())
            {
                var healthState = HealthState.Error;

                try
                {
                    healthState = HealthCheck();
                }
                catch (Exception e)
                {
                    Log.Error(e, GetType().Name);
                }

                healthReporter.ReportHealthRecurrent(Property, healthState, recurrenceInterval + timeToLivePadding);
            }
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
