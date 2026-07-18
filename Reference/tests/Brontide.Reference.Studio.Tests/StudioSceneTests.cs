using Brontide.Reference.Core;
using Brontide.Reference.Studio.Scenes;

namespace Brontide.Reference.Studio.Tests;

public sealed class StudioSceneTests
{
    [Test]
    public async Task Virtual_device_board_makes_attachment_origin_and_masquerade_visible()
    {
        var board = new VirtualDeviceBoardScene();
        var result = await board.RunShowcaseAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Attachment.ActorsCreated.Single().DisplayName, Is.EqualTo("VirtualMouse"));
            Assert.That(result.DeviceMove.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(result.DeviceMove.Events.Single().Interaction.Origin, Is.EqualTo(OriginClass.Device));
            Assert.That(result.MalwareAttempt.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(result.RemoteDesktopMove.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(result.RemoteDesktopMove.Events.Single().Interaction.Origin,
                Is.EqualTo(OriginClass.Unverified));
            Assert.That(result.AcceptedEvents.Length, Is.EqualTo(2));
            Assert.That(board.Domain.Provenance.Any(entry => entry.Kind == ProvenanceKind.Genesis), Is.True);
        });
    }

    [Test]
    public async Task Inspector_exposes_articulate_denials_and_capability_trees()
    {
        var board = new VirtualDeviceBoardScene();
        board.AttachMouse();
        _ = await board.AttemptMalwareInjectionAsync();
        var inspector = new StudioInspector();
        inspector.Refresh(board.Domain);

        Assert.That(inspector.ActorGraph, Does.Contain("VirtualMouse"));
        Assert.That(inspector.CapabilityTrees.Any(line => line.Contains("Input.Pointer.Inject", StringComparison.Ordinal)),
            Is.True);
        Assert.That(inspector.ExecutionLog.Any(line =>
            line.Contains("denied", StringComparison.OrdinalIgnoreCase) &&
            line.Contains("does not designate", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public async Task What_if_toggle_shows_the_worked_attack_succeed_only_when_weakened()
    {
        var secure = await WorkedAttackStudioScene.RunAsync(false);
        var weakened = await WorkedAttackStudioScene.RunAsync(true);

        Assert.Multiple(() =>
        {
            Assert.That(secure.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(secure.Effects, Is.Zero);
            Assert.That(weakened.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(weakened.Effects, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Macro_operation_creates_then_terminally_completes_a_long_lived_activity()
    {
        var result = await MacroOperationScene.RunAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.WorkerCapability.DerivationChain().Length, Is.EqualTo(3));
            Assert.That(result.StartExecution.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(result.StartExecution.Outcome.TerminalFor.Kind, Is.EqualTo(TerminalReferenceKind.Execution));
            Assert.That(result.CompleteExecution.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(result.ActivityOutcome.Status, Is.EqualTo(OutcomeStatus.Completed));
            Assert.That(result.ActivityOutcome.TerminalFor.Kind, Is.EqualTo(TerminalReferenceKind.Activity));
            Assert.That(result.ActivityOutcome.TerminalFor.Value, Is.EqualTo(result.Activity.Value));
            Assert.That(result.Domain.Provenance.Count(entry => entry.Kind == ProvenanceKind.Outcome),
                Is.EqualTo(3));
        });
    }
}
