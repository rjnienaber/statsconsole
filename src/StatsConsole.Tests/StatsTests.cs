using System;
using System.IO;
using NUnit.Framework;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Text.RegularExpressions;

namespace StatsConsole.Tests
{
    [TestFixture]
    public class StatsTests : TestContainer
    {
        [Test]
        public void Should_Serialize_Without_Errors()
        {
            Stats stats = new Stats();
            stats.TimeOperation("GetStocks", "WebService", Console.WriteLine);
            stats.TimeOperation("GetUsers", "Database", Console.WriteLine);

            using (var ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, stats);
            }
        }

        [Test]
        public void Should_Deserialize_Without_Errors()
        {
            Stats stats = new Stats();
            stats.TimeOperation("GetStocks", "WebService", Console.WriteLine);
            stats.TimeOperation("GetUsers", "Database", Console.WriteLine);

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, stats);

                bytes = ms.ToArray();
            }

            Stats deserialized;
            using (var ms = new MemoryStream(bytes))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                deserialized = formatter.Deserialize(ms) as Stats;
            }

            Assert.That(deserialized.Operations.Count, Is.EqualTo(2));
            Assert.That(deserialized.Operations[0].Name, Is.EqualTo("GetStocks"));
            Assert.That(deserialized.Operations[0].Category, Is.EqualTo("WebService"));

            Assert.That(deserialized.Operations[1].Name, Is.EqualTo("GetUsers"));
            Assert.That(deserialized.Operations[1].Category, Is.EqualTo("Database"));
        }

        [Test]
        public void Should_Return_Empty_Stats_When_HttpContext_Is_Null()
        {
            Assert.That(Stats.Current, Is.SameAs(Stats.Empty));

            Stats.Current.TimeOperation("Test", "Test", Console.WriteLine);
            var value = Stats.Current.TimeOperation("Test", "Test", () => "TestValue");
            Assert.That(value, Is.EqualTo("TestValue"));
        }

        [Test]
        public void Should_Output_Well_Formed_Stats()
        {
            Stats stats = new Stats();
            stats.TimeOperation("GetStocks", "WebService", Console.WriteLine);
            stats.TimeOperation("GetUsers", "Database", Console.WriteLine);
            stats.TimeOperation("GetMembers", "Database", Console.WriteLine);

            var markup = stats.WriteOutStatsMarkup(0);

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(markup);
        }

        [Test]
        public void Should_Output_Prescribed_Markup()
        {
            Stats stats = new Stats();

            stats.Operations.Add(new Operation { Name = "GetStocks", Category = "WebService", TotalMilliseconds = 4.3 });
            stats.Operations.Add(new Operation { Name = "GetUsers", Category = "Database", TotalMilliseconds = 0.8 });
            stats.Operations.Add(new Operation { Name = "GetMembers", Category = "Database", TotalMilliseconds = 0.6 });

            var markup = stats.WriteOutStatsMarkup(15.2);

            var noSpacesLeftBracket = Regex.Replace(ReadTestData("reference.html"), ">\\s+", ">");
            var expectedMarkup = Regex.Replace(noSpacesLeftBracket, "\\s+<", "<");
            Assert.That(markup, Is.EqualTo(expectedMarkup));
        }
    }
}
