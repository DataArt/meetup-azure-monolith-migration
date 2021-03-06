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
using Serilog;

namespace DataArt.Atlas.Infrastructure.Startup
{
    public abstract class Startable
    {
        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await StartInternalAsync(cancellationToken);
            Log.Information("Startable {startable} is started", GetType().Name);
        }

        public async Task StopAsync()
        {
            await StopInternalAsync();
            Log.Information("Startable {startable} is stopped", GetType().Name);
        }

        protected virtual Task StartInternalAsync(CancellationToken cancellationToken)
        {
            StartInternal(cancellationToken);
            return Task.FromResult(0);
        }

        protected virtual void StartInternal(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task StopInternalAsync()
        {
            StopInternal();
            return Task.FromResult(0);
        }

        protected virtual void StopInternal()
        {
        }
    }
}
