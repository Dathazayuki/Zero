using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Game.Tests.EditMode
{
    public sealed class EndingTitleCatalogTests
    {
        [TestCase(EndingIdentity.TheCast, "ENDING I — THE CAST")]
        [TestCase(EndingIdentity.AnotherShift, "ENDING II — ANOTHER SHIFT")]
        [TestCase(EndingIdentity.CurtainCall, "TRUE ENDING — CURTAIN CALL")]
        public void EachEndingHasItsCanonicalTitle(EndingIdentity ending, string expected)
        {
            Assert.That(EndingTitleCatalog.GetTitle(ending), Is.EqualTo(expected));
        }

        [Test]
        public void UnknownEndingCannotSilentlyUseAnotherTitle()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                EndingTitleCatalog.GetTitle((EndingIdentity)999));
        }
    }
}
