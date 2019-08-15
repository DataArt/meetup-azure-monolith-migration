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
using System.Linq;
using System.Threading.Tasks;
using DataArt.Atlas.CallContext.Correlation;
using Microsoft.Owin;

namespace DataArt.Atlas.Service.Application.Owin
{
    public class CorrelationIdHandlerMiddleware : OwinMiddleware
    {
        public CorrelationIdHandlerMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override Task Invoke(IOwinContext context)
        {
            string[] correlationIds;

            var value = context.Request.Headers.TryGetValue(CorrelationContext.CorrelationIdName, out correlationIds) ? correlationIds.Single() : null;

            CorrelationContext.SetCorrelationId(value);

            return Next.Invoke(context);
        }
    }
}
