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
using MassTransit;
using Serilog;

namespace DataArt.Atlas.Messaging.MassTransit
{
    internal sealed class ServiceBus : IDisposable, IServiceBus, IInteropServiceBus
    {
        private readonly IBusControl busControl;
        private readonly string name;

        public ServiceBus(IBusControl busControl, string name)
        {
            this.busControl = busControl;
            this.name = name;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return busControl.StartAsync(cancellationToken);
        }

        public Task StopAsync(TimeSpan stopTimeout)
        {
            return busControl.StopAsync(stopTimeout);
        }

        public void Dispose()
        {
            busControl?.Stop();
        }

        public void Publish(object message)
        {
            busControl.Publish(message);
            Log.Debug($"{name} message was published: {{@Message}}", message);
        }

        public Task PublishAsync(object message)
        {
            Log.Debug($"{name} message is publishing: {{@Message}}", message);
            return busControl.Publish(message);
        }
    }
}
