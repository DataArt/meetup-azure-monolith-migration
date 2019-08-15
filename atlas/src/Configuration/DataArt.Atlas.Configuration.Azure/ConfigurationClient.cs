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
using System.IO;
using System.Linq;
using DataArt.Atlas.Configuration.Impl;
using DataArt.Atlas.Configuration.Settings;
using Microsoft.Azure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace DataArt.Atlas.Configuration.Azure
{
    public sealed class ConfigurationClient : IConfigurationClient
    {
        private List<SettingsSection> settingSections;

        public List<SettingsSection> SettingsSections => settingSections ?? (settingSections = ReadConfig<ApplicationSettings>());

        public ApplicationSettings GetApplicationSettings()
        {
            return GetSettings<ApplicationSettings>();
        }

        public T GetSettings<T>() where T : new()
        {
            var settings = SettingsParser.GetSettings<T>(SettingsSections);
            if (settings.Equals(default(T)))
            {
                var additionalSettings = ReadConfig<T>();
                foreach (var additionalSetting in additionalSettings)
                {
                    if (!settingSections.Contains(additionalSetting))
                    {
                        settingSections.Add(additionalSetting);
                    }
                }

                settings = SettingsParser.GetSettings<T>(SettingsSections);
            }

            return settings;
        }
        
        private static List<SettingsSection> ReadConfig<T>()
        {
            var endpoints = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints;
            var sections = new List<SettingsSection>();
            foreach (var property in typeof(T).GetProperties())
            {
                var propertyName = property.Name;
                var section = new SettingsSection(propertyName);
                foreach (var innerProperty in property.PropertyType.GetProperties())
                {
                    var innerPropertyName = innerProperty.Name;
                    var settingName = $"{propertyName}.{innerPropertyName}";
                    var value = CloudConfigurationManager.GetSetting(settingName);
                    if (!string.IsNullOrEmpty(value))
                    {
                        section.Settings.Add(new Setting(innerProperty.Name, value));
                    }
                    else if (endpoints.ContainsKey(settingName))
                    {
                        var endpoint = endpoints[settingName];
                        section.Settings.Add(new Setting(innerProperty.Name, $"{endpoint.Protocol}://{endpoint.IPEndpoint}"));
                    }
                }

                if (section.Settings.Any())
                {
                    sections.Add(section);
                }
            }

            return sections;
        }

        public string GetConfigurationFolderPath()
        {
            var appRoot = Environment.GetEnvironmentVariable("RoleRoot");
            return Path.Combine(appRoot + @"\", @"approot\");
        }
    }
}
