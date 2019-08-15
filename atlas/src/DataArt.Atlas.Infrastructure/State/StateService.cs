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
using System.Threading;
using System.Threading.Tasks;
using DataArt.Atlas.Infrastructure.Startup;
using Serilog;

namespace DataArt.Atlas.Infrastructure.State
{
    public abstract class StateService<T> : Startable, IStateService<T>
    {
        private readonly object lockObject = new object();
        private StateModel<T> currentStateModel;

        public T State => currentStateModel.State;

        protected abstract Task<StateModel<T>> GetInitialStateAsync(CancellationToken cancellationToken);

        protected override async Task StartInternalAsync(CancellationToken cancellationToken)
        {
            Update(await GetInitialStateAsync(cancellationToken));
        }

        public void Update(StateModel<T> stateModel)
        {
            lock (lockObject)
            {
                if (currentStateModel == null || stateModel.UpdatedOn > currentStateModel.UpdatedOn)
                {
                    currentStateModel = stateModel;
                }
                else
                {
                    Log.Warning($"Attempt to update state service {GetType().Name} with an obsolete value: {currentStateModel.UpdatedOn} => {stateModel.UpdatedOn}");
                }
            }
        }
    }
}
