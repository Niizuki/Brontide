using Brontide.Reference.Core;

namespace Brontide.Reference.Core.Tests;

public sealed class CanonicalMemberNameTests
{
    private static readonly object[] CanonicalCases =
    [
        new object[] { "Brontide:Editor.Project#Store.Core", "Brontide:Editor.Project", "Store", "Core" },
        new object[] { "Brontide:Editor.Project#Parameter.HistoryDepth", "Brontide:Editor.Project", "Parameter", "HistoryDepth" },
        new object[] { "Example.Project#ExperimentalKind.Member_1", "Example.Project", "ExperimentalKind", "Member_1" }
    ];

    [TestCaseSource(nameof(CanonicalCases))]
    public void BR_07_NAME_001_typed_members_round_trip_without_becoming_concept_names(
        string text,
        string owner,
        string kind,
        string memberName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(CanonicalMemberName.TryParse(text, out var parsed), Is.True);
            Assert.That(parsed.Owner, Is.EqualTo(CanonicalName.Parse(owner)));
            Assert.That(parsed.Kind, Is.EqualTo(MemberKind.Parse(kind)));
            Assert.That(parsed.Name, Is.EqualTo(MemberName.Parse(memberName)));
            Assert.That(parsed.ToString(), Is.EqualTo(text));
            Assert.That(CanonicalMemberName.Parse(text), Is.EqualTo(parsed));
            Assert.That(CanonicalName.TryParse(text, out _), Is.False);
        });
    }

    [TestCase("")]
    [TestCase(" Brontide:Editor.Project#Store.Core")]
    [TestCase("Brontide:Editor.Project#Store.Core ")]
    [TestCase("#Store.Core")]
    [TestCase("Brontide:Editor.Project#")]
    [TestCase("Brontide:Editor.Project#Store")]
    [TestCase("Brontide:Editor.Project#.Core")]
    [TestCase("Brontide:Editor.Project#Store.")]
    [TestCase("Brontide:Editor.Project#Store.Core.More")]
    [TestCase("Brontide:Editor.Project##Store.Core")]
    [TestCase("Brontide::Editor.Project#Store.Core")]
    [TestCase("Brontide:Editor.Project#Store.Core@3")]
    public void Typed_member_parser_rejects_empty_ambiguous_or_versioned_forms(string text)
    {
        Assert.Multiple(() =>
        {
            Assert.That(CanonicalMemberName.TryParse(text, out _), Is.False);
            Assert.That(() => CanonicalMemberName.Parse(text), Throws.TypeOf<FormatException>());
        });
    }

    [Test]
    public void Member_tokens_are_validated_open_types_and_comparison_is_ordinal()
    {
        var lower = CanonicalMemberName.Parse("Example:Definition#FutureKind.A");
        var upper = CanonicalMemberName.Parse("Example:Definition#FutureKind.B");

        Assert.Multiple(() =>
        {
            Assert.That(MemberKind.TryParse("FutureKind", out _), Is.True);
            Assert.That(MemberKind.TryParse("Future.Kind", out _), Is.False);
            Assert.That(MemberName.TryParse("Member-1", out _), Is.True);
            Assert.That(MemberName.TryParse("Member.Name", out _), Is.False);
            Assert.That(lower.CompareTo(upper), Is.LessThan(0));
        });
    }
}
