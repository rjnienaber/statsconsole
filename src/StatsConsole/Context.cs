#region license
// Copyright 2010 Trafalgar Management Services Licensed under the Apache License,
// Version 2.0 (the "License"); you may not use this file except in compliance with the
// License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
// ANY KIND, either express or implied. See the License for the specific language governing
// permissions and limitations under the License. 
#endregion

using System.Configuration;
using System.Web;

namespace StatsConsole
{
    internal class Context
    {
        private static HttpContextBase _httpContext;
        public static HttpContextBase HttpContext
        {
            get
            {
                if (_httpContext != null) return _httpContext;

                var context = System.Web.HttpContext.Current;
                if (context == null) return null;

                return new HttpContextWrapper(context);
            }
        }

        public static void SetHttpContext(HttpContextBase httpContext)
        {
            _httpContext = httpContext;
        }

        public static bool ShouldRecordStats
        {
            get
            {
                return ConfigurationManager.AppSettings["ShouldShowStats"] == "true";
            }
        }
    }
}
