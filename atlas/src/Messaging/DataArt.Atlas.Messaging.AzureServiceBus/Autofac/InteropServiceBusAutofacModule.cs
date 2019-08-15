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
using Autofac;
using DataArt.Atlas.Configuration.Settings;
using DataArt.Atlas.Messaging.Consume;
using DataArt.Atlas.Messaging.MassTransit;
using DataArt.Atlas.Messaging.MassTransit.Autofac;

namespace DataArt.Atlas.Messaging.AzureServiceBus.Autofac
{
    public class InteropServiceBusAutofacModule : Module
    {
        private readonly Func<IComponentContext, ServiceBusSettings> settingsDelegate;

        public InteropServiceBusAutofacModule(ServiceBusSettings settings)
        {
            settingsDelegate = context => settings;
        }

        public InteropServiceBusAutofacModule(Func<IComponentContext, ServiceBusSettings> settingsDelegate)
        {
            this.settingsDelegate = settingsDelegate;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context =>
            {
                var rootScope = context.Resolve<ILifetimeScope>();

                var bus = AzureServiceBusInitiator.Create(context, settingsDelegate(context),
                    ReceiveEndpointConfiguratorFactory.Create<InteropBusType, DefaultRoutingType>(rootScope),
                    ReceiveEndpointConfiguratorFactory.Create<InteropBusType, FanoutRoutingType>(rootScope));

                return new ServiceBus(bus, "Interop service bus");
            })
            .As<IInteropServiceBus>()
            .SingleInstance();
        }
    }
}
