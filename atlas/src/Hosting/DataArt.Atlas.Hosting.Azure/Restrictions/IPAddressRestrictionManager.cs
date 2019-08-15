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
using System.Diagnostics;
using System.Linq;

namespace DataArt.Atlas.Hosting.Azure.Restrictions
{
    public class IpAddressRestrictionManager
    {
        private static readonly TraceSource Source = new TraceSource(Constants.TraceSource);

        private dynamic firewall;

        private readonly List<string> createdRules;

        private readonly List<string> disabledRules;

        public IpAddressRestrictionManager()
        {
            createdRules = new List<string>();
            disabledRules = new List<string>();
        }

        public void ApplySettings(string settings, bool deleteAllOtherRules = true)
        {
            Source.TraceEvent(TraceEventType.Verbose, 0, "Parsing configuration...");

            IList<Restriction> restrictions = new List<Restriction>();

            foreach (var portDefinition in GetArray(settings, ';'))
            {
                var portAndIp = portDefinition.Split('=');
                if (portAndIp.Length != 2)
                {
                    throw new InvalidOperationException("Invalid format for Restriction: " + portDefinition);
                }

                DisableRules(portAndIp[0]);

                foreach (var ip in GetArray(portAndIp[1], ','))
                {
                    restrictions.Add(new Restriction { Port = portAndIp[0], RemoteAddress = ip });
                }
            }

            foreach (var restriction in restrictions)
            {
                Source.TraceEvent(TraceEventType.Verbose, 0, " > Restriction: {0} - {1}", restriction.RemoteAddress,
                    restriction.Port);
            }

            Apply(restrictions);
        }

        private static IEnumerable<string> GetArray(string text, char separator)
        {
            return text.Contains(separator)
                ? text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                : new[] { text };
        }

        public void Apply(IEnumerable<Restriction> restrictions, bool deleteAllOtherRules = true)
        {
            var rules = GetFirewallRules();
            var rulesToDelete = createdRules.ToList();

            createdRules.Clear();

            var rest = restrictions as Restriction[] ?? restrictions.ToArray();
            Source.TraceEvent(TraceEventType.Verbose, 0, "Applying {0} restrictions", rest.Length);

            foreach (var restriction in rest)
            {
                var name = restriction.ToString();

                var ruleExists = false;
                foreach (var rule in rules)
                {
                    if (rule.Name == name)
                    {
                        ruleExists = true;
                    }
                }

                if (ruleExists)
                {
                    continue;
                }

                AddRule(restriction);

                if (rulesToDelete.Contains(name))
                {
                    rulesToDelete.Remove(name);
                }
            }

            if (deleteAllOtherRules)
            {
                DeleteRules(rulesToDelete);
            }
        }

        public void DeleteRules()
        {
            var rulesToDelete = new List<string>();
            rulesToDelete.AddRange(createdRules);

            var rules = GetFirewallRules();
            foreach (var rule in rules)
            {
                var ruleName = rule.Name as string;
                if (ruleName.StartsWith("WindowsAzure.IPAddressRestriction"))
                {
                    rulesToDelete.Add(ruleName);
                }
            }

            DeleteRules(rulesToDelete);
        }

        public void DeleteRules(List<Restriction> hostnameRestrictions)
        {
            DeleteRules(hostnameRestrictions.Select(o => o.ToString()));
        }

        public void DeleteRules(IEnumerable<string> ruleNames)
        {
            var rules = GetFirewallRules();
            foreach (var rule in rules)
            {
                var ruleName = rule.Name as string;
                if (ruleNames.Contains(ruleName))
                {
                    if (firewall == null)
                    {
                        firewall = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwpolicy2"));
                    }

                    firewall.Rules.Remove(ruleName);

                    if (createdRules.Contains(ruleName))
                    {
                        createdRules.Remove(ruleName);
                    }

                    var localPorts = rule.LocalPorts as string;
                    var remoteAddresses = rule.RemoteAddresses as string;
                    Source.TraceEvent(TraceEventType.Verbose, 0,
                        "Removed rule '{0}' - LocalPorts: {1} - RemoteAddresses: {2}", ruleName, localPorts,
                        remoteAddresses);
                }
            }
        }

        public void AddRule(Restriction restriction)
        {
            var ruleName = restriction.ToString();

            dynamic rule = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwrule"));
            rule.Action = 1;
            rule.Direction = 1;
            rule.Enabled = true;
            rule.InterfaceTypes = "All";
            rule.Name = ruleName;
            rule.Protocol = 6;
            rule.RemoteAddresses = restriction.RemoteAddress;
            rule.LocalPorts = restriction.Port;

            if (firewall == null)
            {
                firewall = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwpolicy2"));
            }

            firewall.Rules.Add(rule);

            if (!createdRules.Contains(ruleName))
            {
                createdRules.Add(ruleName);
            }

            Source.TraceEvent(TraceEventType.Verbose, 0, "Created rule '{0}' - LocalPorts: {1} - RemoteAddresses: {2}",
                ruleName, restriction.Port, restriction.RemoteAddress);
        }

        public void DisableRules(string localPort)
        {
            foreach (var rule in GetFirewallRules())
            {
                var ruleName = rule.Name as string;
                if (rule.Enabled != true || rule.LocalPorts != localPort ||
                    ruleName.StartsWith("WindowsAzure.IPAddressRestriction"))
                {
                    continue;
                }

                rule.Enabled = false;

                if (!disabledRules.Contains(ruleName))
                {
                    disabledRules.Add(ruleName);
                }

                var localPorts = rule.LocalPorts as string;
                var remoteAddresses = rule.RemoteAddresses as string;
                Source.TraceEvent(TraceEventType.Verbose, 0,
                    "Disabled rule '{0}' - LocalPorts: {1} - RemoteAddresses: {2}", ruleName, localPorts,
                    remoteAddresses);
            }
        }

        public void ResetDisabledRules()
        {
            if (disabledRules.Any())
            {
                foreach (var rule in GetFirewallRules())
                {
                    var ruleName = rule.Name as string;
                    if (disabledRules.Contains(ruleName))
                    {
                        rule.Enabled = true;

                        var localPorts = rule.LocalPorts as string;
                        var remoteAddresses = rule.RemoteAddresses as string;
                        Source.TraceEvent(TraceEventType.Verbose, 0,
                            "Re-enabled rule '{0}' - LocalPorts: {1} - RemoteAddresses: {2}", ruleName, localPorts,
                            remoteAddresses);
                    }
                }

                disabledRules.Clear();
            }
        }

        private dynamic GetFirewallRules()
        {
            if (firewall == null)
            {
                firewall = Activator.CreateInstance(Type.GetTypeFromProgID("hnetcfg.fwpolicy2"));
            }

            return firewall.Rules;
        }
    }
}
