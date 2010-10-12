#region license
// Copyright 2010 Trafalgar Management Services Licensed under the Apache License,
// Version 2.0 (the "License"); you may not use this file except in compliance with the
// License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
// ANY KIND, either express or implied. See the License for the specific language governing
// permissions and limitations under the License. 
#endregion

using System;
using System.IO;
using System.Reflection;
using System.Web;

namespace StatsConsole
{
    public class StatsHttpModule : IHttpModule
    {
        private static readonly string StyleSheet;
        private static readonly string Javascript;
        static StatsHttpModule()
        {
            StyleSheet = ReadAsset("styling.css");
            Javascript = ReadAsset("javascript.js");
        }

        static string ReadAsset(string fileName)
        {
            using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("StatsConsole.Assets." + fileName)))
                return sr.ReadToEnd();
        }
        

        public const string STATS_KEY = "Web_Application_Statistics";

        public void Dispose()
        {
            
        }

        public void Init(HttpApplication context)
        {
            if (!Context.ShouldRecordStats) return;

            context.BeginRequest += BeginRequest;
            context.AcquireRequestState += AcquireRequestState;
            context.PostRequestHandlerExecute += PostRequestHandlerExecute;
            context.ReleaseRequestState += ReleaseRequestState;
        }

        public void BeginRequest(object sender, EventArgs e)
        {
            var context = Context.HttpContext;
            var url = context.Request.Url.ToString();
            if (url.Contains("statsconsole-styling.aspx"))
                ServeStaticContent(context, StyleSheet, "text/css");

            if (url.Contains("statsconsole-javascript.aspx"))
                ServeStaticContent(context, Javascript, "text/javascript");

            if (!url.EndsWith(".css") && !url.EndsWith(".js"))
                context.Items[STATS_KEY] = new Stats();
        }

        void ServeStaticContent(HttpContextBase context, string content, string contentType)
        {
            context.Response.Write(content);
            context.Response.ContentType = contentType;
            context.Response.End();
        }

        public void AcquireRequestState(object sender, EventArgs e)
         {
             var context = Context.HttpContext;
             if (context.Request.Url.ToString().EndsWith(".css")) return;

             var session = context.Session;
             if (session == null) return;

             var stats = session[STATS_KEY];
             if (stats == null) return;

             context.Items[STATS_KEY] = stats;
             session.Remove(STATS_KEY);
         }

        public void PostRequestHandlerExecute(object sender, EventArgs e)
        {
            var context = Context.HttpContext;
            
            var response = context.Response;
            if (response.ContentType != "text/html") return;

            if (!IsARedirect(response)) return;

            var stats = context.Items[STATS_KEY];
            if (stats == null) return;

            if (context.Session == null) return;
            context.Session[STATS_KEY] = stats;
        }

        public void ReleaseRequestState(object sender, EventArgs e)
        {
            var context = Context.HttpContext;
            var response = context.Response;
            if (response.ContentType != "text/html" || IsARedirect(response))
                return;

            var stats = context.Items[STATS_KEY] as Stats;
            if (stats == null) return;

            WireUpStatsFilter(stats, response);
        }

        bool IsARedirect(HttpResponseBase response)
        {
            return response.StatusCode == 301 || response.StatusCode == 302;
        }

        public virtual void WireUpStatsFilter(Stats stats, HttpResponseBase response)
        {
            stats.SetResponse(response);
        }
    }
}