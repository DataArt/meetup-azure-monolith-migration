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
using System.Web.Http;
using DataArt.Atlas.Service.Scheduler.Scheduler;
using DataArt.Atlas.Service.Scheduler.Sdk.Models;

namespace DataArt.Atlas.Service.Scheduler.Areas.V1.Controllers
{
    [RoutePrefix("api/v1/scheduler/jobs")]
    public class SchedulerController : ApiController
    {
        private readonly ISchedulerService schedulerService;

        public SchedulerController(ISchedulerService schedulerService)
        {
            this.schedulerService = schedulerService;
        }

        [HttpPost]
        [Route("{id}")]
        public async Task ScheduleJob([FromUri] string id, JobDataModel jobData)
        {
            if (jobData == null)
            {
                throw new ArgumentNullException(nameof(jobData));
            }

            await schedulerService.ScheduleJobAsync(id, jobData);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task DeleteJob(string id)
        {
            await schedulerService.DeleteJobAsync(id);
        }
    }
}
