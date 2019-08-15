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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;

namespace DataArt.Atlas.Service.Application.Http
{
    public static class HttpRequestMessageExtensions
    {
        public static string GetClientIpAddress(this HttpRequestMessage request)
        {
            if (request == null)
            {
                return null;
            }

            if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }

            return null;
        }

        public static HttpResponseMessage CreateFileResponse(this HttpRequestMessage request, byte[] fileData, string responseFileName, string mimeType)
        {
            if (request == null)
            {
                return null;
            }

            return CreateFileResponse(request, new ByteArrayContent(fileData), fileData.Length, responseFileName, mimeType);
        }

        public static HttpResponseMessage CreateFileStreamResponse(this HttpRequestMessage request, Stream stream, string responseFileName, string mimeType)
        {
            if (request == null)
            {
                return null;
            }

            return CreateFileResponse(request, new StreamContent(stream), stream.Length, responseFileName, mimeType);
        }

        public static HttpResponseMessage CreateFileStreamResponse(this HttpRequestMessage request, string tempFileName, string responseFileName, string mimeType)
        {
            if (request == null)
            {
                return null;
            }

            var stream = new FileStream(tempFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return CreateFileStreamResponse(request, stream, responseFileName, mimeType);
        }

        private static HttpResponseMessage CreateFileResponse(HttpRequestMessage request, HttpContent content, long contentLength, string responseFileName, string mimeType)
        {
            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Content = content;

            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = Uri.EscapeUriString(responseFileName);
            response.Content.Headers.ContentDisposition.Size = contentLength;

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            response.Content.Headers.ContentLength = contentLength;

            return response;
        }
    }
}
