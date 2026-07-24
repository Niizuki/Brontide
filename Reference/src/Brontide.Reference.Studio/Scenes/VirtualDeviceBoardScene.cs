using System.Collections.Immutable;
using Brontide.Reference.Core;
using Brontide.Reference.Vocabularies.Input;

namespace Brontide.Reference.Studio.Scenes;

public sealed class VirtualDeviceBoardScene
{
    private readonly List<DomainEvent> _acceptedEvents = [];
    private readonly List<string> _transcript = [];
    private ActorReference? _mouse;
    private Capability? _mouseCapability;

    public VirtualDeviceBoardScene()
    {
        ActorReference inputSystem = null!;
        Domain = AuthorityDomain.Create("Virtual device board", TimeProvider.System, genesis =>
        {
            AttachmentPolicy = genesis.Actor("HostAttachmentPolicy");
            inputSystem = genesis.Actor("HostInputSystem");
            Malware = genesis.Actor("MalwareActor");
            RemoteDesktop = genesis.Actor("RemoteDesktopActor");
            InputPointerVocabulary.Register(genesis, inputSystem, @event => _acceptedEvents.Add(@event));
            RemoteDesktopCapability = genesis.Grant(
                RemoteDesktop,
                inputSystem,
                [InputPointerVocabulary.Inject]);
        });
    }

    public AuthorityDomain Domain { get; }
    public ActorReference AttachmentPolicy { get; private set; } = null!;
    public ActorReference Malware { get; private set; } = null!;
    public ActorReference RemoteDesktop { get; private set; } = null!;
    public Capability RemoteDesktopCapability { get; private set; } = null!;
    public ActorReference? Mouse => _mouse;
    public Capability? MouseCapability => _mouseCapability;
    public IReadOnlyList<DomainEvent> AcceptedEvents => _acceptedEvents.ToArray();
    public IReadOnlyList<string> Transcript => _transcript.ToArray();

    public GenesisRecord AttachMouse()
    {
        if (_mouse is not null)
        {
            throw new InvalidOperationException("The virtual mouse is already attached.");
        }

        var occurrence = Domain.OccurGenesis(
            AttachmentPolicy,
            "Device.Attached",
            "host attachment policy observed a virtual physical pointer device",
            genesis =>
            {
                _mouse = genesis.Actor("VirtualMouse");
                var lease = genesis.Lease(AttachmentPolicy, TimeSpan.FromHours(1));
                _mouseCapability = genesis.Grant(
                    _mouse,
                    RemoteDesktopCapability.Target,
                    [InputPointerVocabulary.Inject],
                    [new LivenessLeaseConstraint(lease), new OriginGrantConstraint(OriginClass.Device)]);
            });
        _transcript.Add("Genesis: VirtualMouse attached with mortal Device-origin authority");
        return occurrence;
    }

    public async ValueTask<ExecutionResult> MoveMouseAsync(long x = 20, long y = 15)
    {
        if (_mouse is null || _mouseCapability is null)
        {
            throw new InvalidOperationException("Attach the virtual mouse first.");
        }

        var result = await Domain.ExecuteAsync(
            _mouse,
            InputPointerVocabulary.Inject,
            _mouseCapability,
            InputPointerVocabulary.MotionValue(x, y),
            OriginClass.Device);
        _transcript.Add($"VirtualMouse: {result.Outcome.Status}, origin {result.Events.Single().Interaction.Origin}");
        return result;
    }

    public async ValueTask<ExecutionResult> AttemptMalwareInjectionAsync(long x = 999, long y = 999)
    {
        var result = await Domain.ExecuteAsync(
            Malware,
            InputPointerVocabulary.Inject,
            RemoteDesktopCapability,
            InputPointerVocabulary.MotionValue(x, y),
            OriginClass.Device);
        _transcript.Add($"MalwareActor: {result.Outcome.Message}");
        return result;
    }

    public async ValueTask<ExecutionResult> MoveRemoteDesktopAsync(long x = 40, long y = 30)
    {
        var result = await Domain.ExecuteAsync(
            RemoteDesktop,
            InputPointerVocabulary.Inject,
            RemoteDesktopCapability,
            InputPointerVocabulary.MotionValue(x, y));
        _transcript.Add($"RemoteDesktopActor: {result.Outcome.Status}, origin {result.Events.Single().Interaction.Origin}");
        return result;
    }

    public async ValueTask<VirtualDeviceBoardResult> RunShowcaseAsync()
    {
        var genesis = AttachMouse();
        var device = await MoveMouseAsync();
        var malware = await AttemptMalwareInjectionAsync();
        var remote = await MoveRemoteDesktopAsync();
        return new VirtualDeviceBoardResult(
            genesis,
            device,
            malware,
            remote,
            _acceptedEvents.ToImmutableArray(),
            _transcript.ToImmutableArray());
    }
}

public sealed record VirtualDeviceBoardResult(
    GenesisRecord Attachment,
    ExecutionResult DeviceMove,
    ExecutionResult MalwareAttempt,
    ExecutionResult RemoteDesktopMove,
    ImmutableArray<DomainEvent> AcceptedEvents,
    ImmutableArray<string> Transcript);
