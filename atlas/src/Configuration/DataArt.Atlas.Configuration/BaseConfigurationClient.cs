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
using System.Collections.Concurrent;
using System.Collections.Generic;
using DataArt.Atlas.Configuration.Impl;
using DataArt.Atlas.Configuration.Settings;

namespace DataArt.Atlas.Configuration
{
    public abstract class BaseConfigurationClient : IConfigurationClient
    {
        private List<SettingsSection> settingSections;

        private readonly ConcurrentDictionary<Type, object> settings;

        public List<SettingsSection> SettingsSections => settingSections ?? (settingSections = ReadConfig());

        protected BaseConfigurationClient()
        {
            settings = new ConcurrentDictionary<Type, object>();    
        }

        protected abstract List<SettingsSection> ReadConfig();

        public T GetSettings<T>() where T : new()
        {
            return (T)settings.GetOrAdd(typeof(T), type => LoadSettings<T>());
        }

        public ApplicationSettings GetApplicationSettings()
        {
            return GetSettings<ApplicationSettings>();
        }

        public abstract string GetConfigurationFolderPath();

        private object LoadSettings<T>() where T : new()
        {
            return SettingsParser.GetSettings<T>(SettingsSections);
        }
    }
}
