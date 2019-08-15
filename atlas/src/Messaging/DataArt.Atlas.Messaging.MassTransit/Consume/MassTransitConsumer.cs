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
using DataArt.Atlas.Messaging.Consume;
using Serilog;

namespace DataArt.Atlas.Messaging.MassTransit.Consume
{
    internal class MassTransitConsumer<TBusType, TRoutingType, TMessage> : IMassTransitConsumer<TBusType, TRoutingType, TMessage>
        where TMessage : class, new()
        where TBusType : class, IConsumerBusType
        where TRoutingType : class, IConsumerRountingType
    {
        private readonly IConsumer<TBusType, TRoutingType, TMessage> consumer;

        public MassTransitConsumer(IConsumer<TBusType, TRoutingType, TMessage> consumer)
        {
            this.consumer = consumer;
        }

        public async Task Consume(global::MassTransit.ConsumeContext<TMessage> context)
        {
            try
            {
                await consumer.Consume(context.Message);
                Log.Debug("Service bus message was consumed: {@Message}", context.Message);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Service bus message consuming failed {@Message}", context.Message);
                throw;
            }
        }
    }
}
