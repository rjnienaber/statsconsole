using System;
using NUnit.Framework;
using Moq;
using System.Web;
using System.Collections;

namespace StatsConsole.Tests
{
    [TestFixture]
    public class StatsHttpModuleTests : TestContainer
    {
        private Mock<HttpContextBase> _context;
        private Mock<StatsHttpModule> _module;
        private Mock<HttpRequestBase> _request;
        private Mock<HttpSessionStateBase> _session;
        private Mock<HttpResponseBase> _response;
        private Hashtable _items;

        public override void Setup()
        {
            _request = Factory.Create<HttpRequestBase>();
            _items = new Hashtable();
            _session = Factory.Create<HttpSessionStateBase>();
            _response = Factory.Create<HttpResponseBase>();

            _context = Factory.Create<HttpContextBase>();
            _context.Setup(c => c.Request).Returns(_request.Object);
            _context.Setup(c => c.Response).Returns(_response.Object);

            _module = Factory.Create<StatsHttpModule>();
            _module.CallBase = true;
            Context.SetHttpContext(_context.Object);
        }

        public override void Teardown()
        {
            Context.SetHttpContext(null);
        }

        [Test]
        public void Should_Not_Copy_To_Session_State_When_Not_A_Redirect_Response()
        {
            _request.Setup(r => r.Url).Returns(new Uri("http://localhost/default.aspx"));
            _context.Setup(c => c.Items).Returns(_items);
            _response.Setup(r => r.ContentType).Returns("text/html");

            Assert.That(_items.Count, Is.EqualTo(0));
            _module.Object.BeginRequest(null, null);
            Assert.That(_items.Count, Is.EqualTo(1));
            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.Not.Null);
            var stats = _items[StatsHttpModule.STATS_KEY];

            _context.Setup(c => c.Session).Returns(_session.Object);
            _session.Setup(s => s[StatsHttpModule.STATS_KEY]).Returns((Stats) null);
            _module.Object.AcquireRequestState(null, null);
            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.Not.Null);
            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.SameAs(stats));

            _response.Setup(r => r.StatusCode).Returns(200);
            _module.Object.PostRequestHandlerExecute(null, null);

            _module.Setup(m => m.WireUpStatsFilter(It.IsAny<Stats>(), _response.Object));
            _module.Object.ReleaseRequestState(null, null);
        }

        [Test]
        public void Should_Copy_To_Session_State_When_A_Redirect_Response()
        {
            _request.Setup(r => r.Url).Returns(new Uri("http://localhost/default.aspx"));
            _context.Setup(c => c.Items).Returns(_items);
            _response.Setup(r => r.ContentType).Returns("text/html");

            Assert.That(_items.Count, Is.EqualTo(0));
            _module.Object.BeginRequest(null, null);
            Assert.That(_items.Count, Is.EqualTo(1));
            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.Not.Null);
            var stats = _items[StatsHttpModule.STATS_KEY];

            _context.Setup(c => c.Session).Returns(_session.Object);
            _session.Setup(s => s[StatsHttpModule.STATS_KEY]).Returns((Stats)null);
            _module.Object.AcquireRequestState(null, null);

            _session.SetupSet(s => s[StatsHttpModule.STATS_KEY] = stats);
            _response.Setup(r => r.StatusCode).Returns(302);
            _module.Object.PostRequestHandlerExecute(null, null);

            _module.Object.ReleaseRequestState(null, null);
        }

        [Test]
        public void Should_Overwrite_Items_Stats_Object_With_Stats_Object_From_Session()
        {
            _request.Setup(r => r.Url).Returns(new Uri("http://localhost/default.aspx"));
            _context.Setup(c => c.Items).Returns(_items);
            _response.Setup(r => r.ContentType).Returns("text/html");

            Assert.That(_items.Count, Is.EqualTo(0));
            _module.Object.BeginRequest(null, null);
            Assert.That(_items.Count, Is.EqualTo(1));
            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.Not.Null);

            _context.Setup(c => c.Session).Returns(_session.Object);
            var sessionStats = new Stats();
            _session.Setup(s => s[StatsHttpModule.STATS_KEY]).Returns(sessionStats);
            _session.Setup(s => s.Remove(StatsHttpModule.STATS_KEY));
            _module.Object.AcquireRequestState(null, null);
            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.Not.Null);
            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.SameAs(sessionStats));

            _response.Setup(r => r.StatusCode).Returns(200);
            _module.Object.PostRequestHandlerExecute(null, null);

            _module.Setup(m => m.WireUpStatsFilter(It.IsAny<Stats>(), _response.Object));
            _module.Object.ReleaseRequestState(null, null);
        }

        [Test]
        public void Should_Return_Stats_Stylesheet_If_Request_Detected()
        {
            _request.Setup(r => r.Url).Returns(new Uri("http://localhost/statsconsole-styling.aspx"));

            _response.SetupSet(r => r.ContentType = "text/css");
            _response.Setup(r => r.End()).Throws(new Exception());
            _response.Setup(r => r.Write(It.IsAny<string>()));
            Assert.That(_items.Count, Is.EqualTo(0));

            Assert.Throws<Exception>(() => _module.Object.BeginRequest(null, null));
            
            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.Null);
        }

        [Test]
        public void Should_Return_Stats_Javascript_If_Request_Detected()
        {
            _request.Setup(r => r.Url).Returns(new Uri("http://localhost/statsconsole-javascript.aspx"));

            _response.SetupSet(r => r.ContentType = "text/javascript");
            _response.Setup(r => r.End()).Throws(new Exception());
            _response.Setup(r => r.Write(It.IsAny<string>()));
            Assert.That(_items.Count, Is.EqualTo(0));

            Assert.Throws<Exception>(() => _module.Object.BeginRequest(null, null));

            Assert.That(_items[StatsHttpModule.STATS_KEY], Is.Null);
        }

        [Test]
        public void Should_Only_Handle_Html_Requests()
        {
            _request.Setup(r => r.Url).Returns(new Uri("http://localhost/Site.css"));
            
            _context.Setup(c => c.Items).Returns(_items);

            Assert.That(_items.Count, Is.EqualTo(0));
            _module.Object.BeginRequest(null, null);
            Assert.That(_items.Count, Is.EqualTo(0));

            _module.Object.AcquireRequestState(null, null);

            _response.Setup(r => r.StatusCode).Returns(200);
            _response.Setup(r => r.ContentType).Returns("text/html");
            _module.Object.PostRequestHandlerExecute(null, null);
            _module.Object.ReleaseRequestState(null, null);
        }
    }
}
