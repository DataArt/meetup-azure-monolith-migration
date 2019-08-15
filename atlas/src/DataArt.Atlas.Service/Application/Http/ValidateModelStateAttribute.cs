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
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.ModelBinding;
using DataArt.Atlas.Infrastructure.Exceptions;

namespace DataArt.Atlas.Service.Application.Http
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (!actionContext.ModelState.IsValid)
            {
                throw new ApiValidationException(ErrorMessage(actionContext.ModelState));
            }
        }

        private static string ErrorMessage(ModelStateDictionary state)
        {
            const string Separator = ", ";
            return string.Join(Separator, state.Values.SelectMany(x => x.Errors).Select(x => string.IsNullOrEmpty(x.ErrorMessage)
            ? x.Exception is Newtonsoft.Json.JsonException ? "The request is invalid" : x.Exception?.Message // we don't want to show parsing errors due to security reasons
            : x.ErrorMessage));
        }
    }
}
