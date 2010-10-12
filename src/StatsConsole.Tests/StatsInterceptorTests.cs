using NUnit.Framework;

namespace StatsConsole.Tests
{
    [TestFixture]
    public class StatsInterceptorTests : TestContainer
    {
        [Test]
        public void Should_Not_Wrap_If_StatsModule_Not_Loaded()
        {
            var concreteClass = StatsInterceptor.Wrap<IStats, EmptyStats>("Test");

            Assert.That(concreteClass.GetType(), Is.EqualTo(typeof (EmptyStats)));
        }

        [Test]
        public void Should_Not_Wrap_Interface_If_StatsModule_Not_Loaded()
        {
            var testInterface = Factory.Create<IStats>();

            var @inteface = StatsInterceptor.Wrap<IStats>(testInterface.Object, "Test");

            Assert.That(@inteface, Is.SameAs(testInterface.Object));
        }
    }
}
