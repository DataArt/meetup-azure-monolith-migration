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
using Microsoft.Owin;

namespace DataArt.Atlas.Service.Application.Logging
{
    public class OwinUnhandledExceptionEvent
    {
        public string QueryString { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string Host { get; set; }

        public int? ResponseStatusCode { get; set; }

        public bool IsCancellationRequested { get; set; }

        public OwinUnhandledExceptionEvent(IOwinRequest request, IOwinResponse response = null)
        {
            IsCancellationRequested = request.CallCancelled.IsCancellationRequested;

            Host = request.Host.ToString();
            Path = request.Path.ToString();
            Method = request.Method;
            QueryString = request.QueryString.ToString();

            ResponseStatusCode = response?.StatusCode;
        }
    }
}
