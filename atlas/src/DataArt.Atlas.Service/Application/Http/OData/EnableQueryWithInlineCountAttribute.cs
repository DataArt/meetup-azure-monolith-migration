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
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.OData;
using DataArt.Atlas.Infrastructure.Exceptions;
using DataArt.Atlas.Infrastructure.OData;

namespace DataArt.Atlas.Service.Application.Http.OData
{
    public class EnableQueryWithInlineCountAttribute : EnableQueryAttribute
    {
        private const string InlineCountPropertyKey = "MS_InlineCount";

        private const string InlineCountParamName = "$inlinecount";
        private const string InlineCountParamValue = "allpages";

        private const string TopParamName = "$top";
        private const int TopParamMinValue = 1;
        private const int TopParamMaxValue = 100;

        private const string SkipParamName = "$skip";

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var queryParams = actionContext.Request.GetODataParams().ToDictionary(k => k.Key.ToLower(), v => v.Value.ToLower());

            if (!queryParams.ContainsKey(TopParamName))
            {
                throw new ApiValidationException($"Invalid query: {TopParamName} must be specified");
            }

            int topValue;

            if (!int.TryParse(queryParams[TopParamName], out topValue) || topValue > TopParamMaxValue || topValue < TopParamMinValue)
            {
                throw new ApiValidationException($"Invalid query: {TopParamName} must be in range {TopParamMinValue}-{TopParamMaxValue}");
            }

            int skipValue;

            if (queryParams.ContainsKey(SkipParamName) && (!int.TryParse(queryParams[SkipParamName], out skipValue) || skipValue < 0))
            {
                throw new ApiValidationException($"Invalid query: {SkipParamName} must be positive");
            }

            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var queryParams = actionExecutedContext.Request.GetODataParams().ToDictionary(k => k.Key.ToLower(), v => v.Value.ToLower());

            base.OnActionExecuted(actionExecutedContext);

            object responseObject;
            if (ResponseIsValid(actionExecutedContext.Response)
                && queryParams.ContainsKey(InlineCountParamName)
                && queryParams[InlineCountParamName].Equals(InlineCountParamValue, StringComparison.CurrentCultureIgnoreCase)
                && actionExecutedContext.Request.Properties.ContainsKey(InlineCountPropertyKey)
                && actionExecutedContext.Response.TryGetContentValue(out responseObject))
            {
                actionExecutedContext.Response = actionExecutedContext.Request.CreateResponse(HttpStatusCode.OK, new { Items = responseObject, Count = actionExecutedContext.Request.Properties[InlineCountPropertyKey] });
            }
        }

        private static bool ResponseIsValid(HttpResponseMessage response)
        {
            return response != null && response.IsSuccessStatusCode && response.Content is ObjectContent;
        }
    }
}
