using System;
using System.Web.Mvc;
using System.Threading;
using StatsConsole;

namespace DemoApp.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var stats = StatsInterceptor.Wrap<IStats>(Stats.Current, "StatsTest");

            stats.TimeOperation("GetProducts", "WebService", () => Thread.Sleep(DateTime.Now.Millisecond % 100));
            stats.TimeOperation("GetUser", "MSSQL", () => Thread.Sleep(DateTime.Now.Millisecond % 100));
            stats.TimeOperation("WriteUpdate", "Oracle", () => Thread.Sleep(DateTime.Now.Millisecond % 100));

            ViewData["Message"] = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
