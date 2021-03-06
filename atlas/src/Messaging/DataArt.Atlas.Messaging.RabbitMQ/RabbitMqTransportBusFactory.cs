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
using System.Linq;
using Autofac;
using DataArt.Atlas.Configuration.Settings;
using DataArt.Atlas.Messaging.MassTransit.Intercept;
using MassTransit;

namespace DataArt.Atlas.Messaging.RabbitMq
{
    internal static class RabbitMqTransportBusFactory
    {
        private static readonly char SettingsDelimeter = ';';

        public static IBusControl Create(
            IComponentContext context,
            ServiceBusSettings settings,
            Action<IReceiveEndpointConfigurator> receiveEndpointConfigurator,
            Action<IReceiveEndpointConfigurator> fanoutReceiveEndpointConfigurator)
        {
            return Bus.Factory.CreateUsingRabbitMq(x =>
            {
                x.UseSerilog();

                var host = x.Host(new Uri(GetHostUrl(settings)), h =>
                {
                    h.Username(settings.RabbitUsername);
                    h.Password(settings.RabbitPassword);

                    var nodes = GetClusterNodes(settings);

                    if (nodes.Any())
                    {
                        h.UseCluster(c =>
                        {
                            foreach (var node in nodes)
                            {
                                c.Node(node);
                            }
                        });
                    }
                });

                if (receiveEndpointConfigurator != null)
                {
                    x.ReceiveEndpoint(settings.QueueName, receiveEndpointConfigurator);
                }

                if (fanoutReceiveEndpointConfigurator != null)
                {
                    x.ReceiveEndpoint(host, fanoutReceiveEndpointConfigurator);
                }

                x.ConfigurePublish(cfg => cfg.UseSendExecute(sendContext => sendContext.ApplyInterceptors()));
            });
        }

        private static string GetHostUrl(ServiceBusSettings settings)
        {
            return settings.RabbitUrl.Split(new[] { SettingsDelimeter }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        private static string[] GetClusterNodes(ServiceBusSettings settings)
        {
            var uris = settings.RabbitUrl.Split(new[] { SettingsDelimeter }, StringSplitOptions.RemoveEmptyEntries);
            return uris.Skip(1).ToArray();
        }
    }
}
