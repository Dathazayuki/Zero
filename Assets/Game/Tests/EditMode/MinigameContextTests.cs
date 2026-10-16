using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class MinigameContextTests
    {
        [Test]
        public void ContextDoesNotDiscoverOrCreatePlayerReferences()
        {
            var context = new MinigameContext(null, null, null);
            Assert.IsNull(context.Player);
            Assert.IsNull(context.Fade);
            Assert.IsNull(context.Entrance);
        }
    }
}
