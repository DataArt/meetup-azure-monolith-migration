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
using System.Net;
using System.Net.Http;
using DataArt.Atlas.Service.Application.Http;

namespace DataArt.Atlas.Service.Application.Logging
{
    public class ApiAccessEvent
    {
        public DateTime AccessTime { get; set; }

        public IDictionary<string, object> ActionArguments { get; set; }

        public string ActionName { get; set; }

        public string ControllerName { get; set; }

        public string HttpMethod { get; set; }

        public string RemoteIpAddress { get; set; }

        public long RequestExecutionTime { get; set; }

        public string RequestUri { get; set; }

        public HttpStatusCode? ResponseStatusCode { get; set; }

        public string RouteTemplate { get; set; }

        public string UserAgent { get; set; }

        public ApiAccessEvent(HttpRequestMessage request, HttpResponseMessage response = null, long? requestExecutionTime = null)
        {
            AccessTime = DateTime.Now;

            var routeData = request.GetRouteData();

            if (requestExecutionTime.HasValue)
            {
                RequestExecutionTime = requestExecutionTime.Value;
            }

            ResponseStatusCode = response?.StatusCode;

            RequestUri = request.RequestUri.AbsoluteUri;
            RemoteIpAddress = request.GetClientIpAddress();
            HttpMethod = request.Method.Method;

            RouteTemplate = routeData?.Route.RouteTemplate ?? string.Empty;
            UserAgent = request.Headers.UserAgent?.ToString() ?? string.Empty;

            var actionDescriptor = request.GetActionDescriptor();
            if (actionDescriptor != null)
            {
                ControllerName = actionDescriptor.ControllerDescriptor.ControllerName;
                ActionName = actionDescriptor.ActionName;
            }
        }
    }
}
