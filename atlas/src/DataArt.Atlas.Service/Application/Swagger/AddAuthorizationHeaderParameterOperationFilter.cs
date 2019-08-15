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
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;
using DataArt.Atlas.Service.Application.Http;
using Swashbuckle.Swagger;

namespace DataArt.Atlas.Service.Application.Swagger
{
    internal class AddAuthorizationHeaderParameterOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var shouldBeAuthorized = !apiDescription.GetControllerAndActionAttributes<AuthorizeDisabledAttribute>().Any();

            if (shouldBeAuthorized)
            {
                if (operation.parameters == null)
                {
                    operation.parameters = new List<Parameter>();
                }

                operation.parameters.Add(new Parameter
                {
                    name = "Authorization",
                    @in = "header",
                    description = "Access Token",
                    required = true,
                    type = "string"
                });
            }
        }
    }
}
