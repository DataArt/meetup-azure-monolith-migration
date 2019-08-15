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
using System.Reflection;
using System.Xml.Linq;
using DataArt.Atlas.Configuration.Impl;

namespace DataArt.Atlas.Configuration.File
{
    public sealed class ConfigurationClient : BaseConfigurationClient
    {
        private readonly string configurationFileName;

        public ConfigurationClient()
        {
            configurationFileName = "Settings.xml";
        }

        public ConfigurationClient(string configurationFileName)
        {
            this.configurationFileName = configurationFileName;
        }

        protected override List<SettingsSection> ReadConfig()
        {
            var configurationFilePath = Path.Combine(GetConfigurationFolderPath(), configurationFileName);
            if (!System.IO.File.Exists(configurationFilePath))
            {
                throw new ApplicationException($"Unable to find configuration file {configurationFilePath}");
            }

            var document = XDocument.Load(configurationFilePath);
            var parsedSections = new List<SettingsSection>();
            
            // ReSharper disable PossibleNullReferenceException
            var nmspc = document.Root.Attribute("xmlns").Value;
            foreach (var section in document.Descendants(XName.Get("Section", nmspc)))
            {
                var parsedSection = new SettingsSection(section.Attribute("Name").Value);

                foreach (var setting in section.Descendants(XName.Get("Parameter", nmspc)))
                {
                    parsedSection.Settings.Add(new Setting(setting.Attribute("Name").Value, setting.Attribute("Value").Value));
                }

                parsedSections.Add(parsedSection);
            }
            // ReSharper enable PossibleNullReferenceException

            return parsedSections;
        }

        public override string GetConfigurationFolderPath()
        {
            return $"{Assembly.GetEntryAssembly().GetName().Name}.Config";
        }
    }
}
