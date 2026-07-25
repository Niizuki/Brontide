using Brontide.Reference.Core;

namespace Brontide.Reference.Core.Tests;

public sealed class ActorReferenceTests
{
    [Test]
    public void Actor_references_are_opaque_and_reference_comparable()
    {
        ActorReference first = null!;
        ActorReference second = null!;
        _ = AuthorityDomain.Create("actors", genesis =>
        {
            first = genesis.Actor("same display name");
            second = genesis.Actor("same display name");
        });

        Assert.That(typeof(ActorReference).GetConstructors(), Is.Empty);
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(ReferenceEquals(first, first), Is.True);
    }
}
