using Fabric.Core;

namespace Fabric.Vocabularies.Input;

/// <summary>Minimal Input.Pointer vocabulary required by the virtual-device showcase.</summary>
public static class InputPointerVocabulary
{
    public static readonly OperationReference Inject = OperationReference.Parse("Input.Pointer.Inject");
    public static readonly EventReference Motion = EventReference.Parse("Input.Pointer.Motion");
    public static readonly ShapeReference MotionShape = ShapeReference.Parse("Input.Pointer.Motion", 1);

    public static void Register(
        AuthorityDomain.GenesisContext genesis,
        ActorReference inputSystem,
        Action<DomainEvent>? acceptedMotion = null)
    {
        ArgumentNullException.ThrowIfNull(genesis);
        ArgumentNullException.ThrowIfNull(inputSystem);
        genesis.Shape(ShapeDefinition.Record(
            MotionShape,
            FragmentPolicy.Open,
            RecordField.Required("x", BuiltInShapes.Signed64),
            RecordField.Required("y", BuiltInShapes.Signed64)));
        genesis.Event(
            Motion,
            ShapeContract.For(MotionShape),
            "An immutable attributable assertion of pointer movement; receipt grants no reactive authority.");
        genesis.Operation(
            Inject,
            inputSystem,
            ShapeContract.For(MotionShape),
            ShapeContract.Unit,
            "Admit a pointer movement into the host input system under explicit injection and origin authority.",
            context =>
            {
                var emitted = context.EmitEventFromInitiator(
                    Motion,
                    context.Input,
                    context.Execution.Interaction.Origin,
                    context.Execution.AuthorityPresentation.Capability);
                acceptedMotion?.Invoke(emitted);
                return OperationEffect.SucceededAsync(ShapeValue.Unit, "pointer movement admitted");
            });
    }

    public static ShapeValue MotionValue(long x, long y) => ShapeValue.Record(
        MotionShape,
        ("x", ShapeValue.Signed64(x)),
        ("y", ShapeValue.Signed64(y)));
}
