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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace StatsConsole
{
    [Serializable]
    public class Stats : IStats
    {
        private readonly long RequestStartTime = Stopwatch.GetTimestamp();
        readonly Lazy<List<Operation>> _lazyOperations = new Lazy<List<Operation>>(() => new List<Operation>());
        internal List<Operation> Operations 
        {
            get { return _lazyOperations.Value; }
        }

        internal void InsertStatsStyling(StreamWriter stream)
        {
            stream.Write("<link href=\"statsconsole-styling.aspx\" rel=\"styleSheet\" type=\"text/css\" />");
        }

        internal void InsertStats(StreamWriter stream)
        {
            //calculating it manually because stop watch is not serializable
            var rawElapsedTicks = Stopwatch.GetTimestamp() - RequestStartTime;
            var ticks = rawElapsedTicks/ (double)Stopwatch.Frequency;
            var totalTimeSpan = new TimeSpan((long) (ticks*10000000));
            
            OutputToStream(stream, totalTimeSpan.TotalMilliseconds);
        }

        private void OutputToStream(StreamWriter stream, double totalMilliseconds)
        {
            var expandableIndicator = Operations.Count > 0 ? "+" : "";

            var module = new XElement("div", new XAttribute("class", "stats-module"),
                            new XElement("div", new XAttribute("class", "stats-module-total"),
                                new XElement("span", new XAttribute("class", "stats-module-expandTotal"), expandableIndicator),
                                  "Total time elapsed: " + (totalMilliseconds.ToString("0.0 ms"))
                            )
                        );

            if (Operations.Count > 0)
            {
                module.Add(GenerateCategories(totalMilliseconds));
                module.Add(WriteJavaScript("http://ajax.googleapis.com/ajax/libs/jquery/1.4.2/jquery.min.js"));
                module.Add(WriteJavaScript("statsconsole-javascript.aspx"));
            }

            stream.Write(module.ToString(SaveOptions.DisableFormatting));
        }

        private XElement GenerateCategories(double totalMilliseconds)
        {
            var categories = GetElement("div", "stats-module-categories", "display: none;");

            var top = (from o in Operations
                       orderby o.TotalMilliseconds descending
                       select o).Take(5).ToList();

            if (top.Count > 0)
                categories.Add(GenerateCategory("Top " + top.Count, top, totalMilliseconds, true));

            var categoryElements = from o in Operations
                                   group o by o.Category into g
                                   let operations = g.OrderBy(i => i.Name)
                                   orderby g.Key
                                   select GenerateCategory(g.Key, operations, totalMilliseconds, false);
            
            categories.Add(categoryElements);

            return categories;
        }

        XElement GenerateCategory(string category, IEnumerable<Operation> operations, double totalMilliseconds, bool addCategoryToName)
        {
            var categoryStats = GetElement("table", "stats-module-category-stats", "display: none;");
            foreach (var o in operations.OrderByDescending(i => i.TotalMilliseconds))
            {
                var tdText = CalculatePercentage("", "", o.TotalMilliseconds, totalMilliseconds);
                var operationName = addCategoryToName ? string.Concat(o.Name, " (", o.Category, ")") : o.Name;
                categoryStats.Add(new XElement("tr", new XElement("td", operationName), new XElement("td", tdText)));
            }

            var categoryTotal = (operations.Sum(o => o.TotalMilliseconds));
            var categoryText = CalculatePercentage("Category: ", category + " ", categoryTotal, totalMilliseconds);

            return new XElement("div", new XAttribute("class", "stats-module-category"),
                      WriteSpan("+", "stats-module-expandCategory"),
                      WriteSpan(categoryText, "stats-module-category-title"),
                      categoryStats
                   );
        }

        string CalculatePercentage(string preText, string text, double dividend, double divisor)
        {
            return string.Concat(preText, text, dividend.ToString("0.0 ms"), " (", (dividend / divisor * 100).ToString("0.0\\%"), ")");
        }

        XElement WriteSpan(string innertText, string @class)
        {
            return new XElement("span", new XAttribute("class", @class), innertText);
        }

        XElement WriteJavaScript(string src)
        {
            return new XElement("script", new XAttribute("type", "text/javascript"), new XAttribute("src", src), "");
        }

        XElement GetElement(string element, string @class, string style)
        {
            return new XElement(element, new XAttribute("class", @class), new XAttribute("style", style));
        }

        internal string WriteOutStatsMarkup(double totalMilliseconds)
        {
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    OutputToStream(sw, totalMilliseconds);
                    sw.Flush();
                    return sw.Encoding.GetString(ms.ToArray());
                }
            }
        }

        public void SetResponse(HttpResponseBase response)
        {
            InsertMarkupStream filter = new InsertMarkupStream(response);
            filter.EndOfBodyDetected += InsertStats;
            filter.EndOfHeadDetected += InsertStatsStyling;
            response.Filter = filter;
        }

        public void TimeOperation(string name, string category, Action action)
        {
            Stopwatch watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            Operations.Add(new Operation { Category = category, Name = name, TotalMilliseconds = watch.Elapsed.TotalMilliseconds });
        }

        public T TimeOperation<T>(string name, string category, Func<T> func)
        {
            var result = default(T);
            TimeOperation(name, category, (Action)(() => result = func()));
            return result;
        }

        internal readonly static EmptyStats Empty = new EmptyStats();

        public static IStats Current
        {
            get
            {
                var context = Context.HttpContext;
                if (context == null) return Empty;

                return context.Items[StatsHttpModule.STATS_KEY] as IStats ?? Empty;
            }
        }
    }
}