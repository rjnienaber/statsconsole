using System.IO;
using System.Reflection;
using Moq;
using NUnit.Framework;

namespace StatsConsole.Tests
{
    public class TestContainer
    {
        protected MockFactory Factory { get; set; }

        [SetUp]
        public void TestSetup()
        {
            Factory = new MockFactory(MockBehavior.Strict);
            Setup();
        }

        public virtual void Setup() { }

        [TearDown]
        public void TestTeardown()
        {
            try
            {
                Teardown();
            }
            finally
            {
                Factory.VerifyAll();
            }
        }

        public virtual void Teardown() { }

        public string ReadTestData(string fileName)
        {
            using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("StatsConsole.Tests.TestData." + fileName)))
                return sr.ReadToEnd();
        }
    }
}
