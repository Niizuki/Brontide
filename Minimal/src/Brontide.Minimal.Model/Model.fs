namespace Brontide.Minimal.Model

open System

[<StructuralEquality; StructuralComparison>]
type CanonicalName = private CanonicalName of string

[<RequireQualifiedAccess>]
module CanonicalName =
    let value (CanonicalName value) = value

    let tryCreate (value: string) =
        let validCharacter character =
            Char.IsLetterOrDigit character || character = '-' || character = '_'

        let validPath (path: string) =
            not (String.IsNullOrWhiteSpace path)
            && not (path.StartsWith('.') || path.EndsWith('.'))
            && (path.Split('.')
                |> Array.forall (fun segment ->
                    not (String.IsNullOrWhiteSpace segment)
                    && (segment |> Seq.forall validCharacter)))

        if String.IsNullOrWhiteSpace value then
            Error "A canonical name cannot be empty."
        else
            match value.Split(':') with
            | [| conceptPath |] when validPath conceptPath -> Ok(CanonicalName value)
            | [| authorityPath; conceptPath |] when validPath authorityPath && validPath conceptPath ->
                Ok(CanonicalName value)
            | _ ->
                Error
                    "A canonical name must be ConceptPath or AuthorityPath:ConceptPath with valid dot-separated segments."

    let create value =
        match tryCreate value with
        | Ok name -> name
        | Error message -> invalidArg (nameof value) message

[<Struct; StructuralEquality; StructuralComparison>]
type ActorReference = private ActorReference of scope: Guid * value: int64

[<RequireQualifiedAccess>]
module ActorReference =
    let internal issue scope value = ActorReference(scope, value)
    let scope (ActorReference(scope, _)) = scope
    let value (ActorReference(_, value)) = value

[<Struct; StructuralEquality; StructuralComparison>]
type CapabilityReference = private CapabilityReference of scope: Guid * value: int64

[<RequireQualifiedAccess>]
module CapabilityReference =
    let internal issue scope value = CapabilityReference(scope, value)
    let scope (CapabilityReference(scope, _)) = scope
    let value (CapabilityReference(_, value)) = value

[<Struct; StructuralEquality; StructuralComparison>]
type ConstraintReference = private ConstraintReference of scope: Guid * value: int64

[<RequireQualifiedAccess>]
module ConstraintReference =
    let internal issue scope value = ConstraintReference(scope, value)
    let scope (ConstraintReference(scope, _)) = scope
    let value (ConstraintReference(_, value)) = value

[<Struct; StructuralEquality; StructuralComparison>]
type ExecutionReference = private ExecutionReference of scope: Guid * value: int64

[<RequireQualifiedAccess>]
module ExecutionReference =
    let internal issue scope value = ExecutionReference(scope, value)
    let scope (ExecutionReference(scope, _)) = scope
    let value (ExecutionReference(_, value)) = value

[<Struct; StructuralEquality; StructuralComparison>]
type OccurrenceReference = private OccurrenceReference of scope: Guid * value: int64

[<RequireQualifiedAccess>]
module OccurrenceReference =
    let internal issue scope value = OccurrenceReference(scope, value)
    let scope (OccurrenceReference(scope, _)) = scope
    let value (OccurrenceReference(_, value)) = value

[<Struct; StructuralEquality; StructuralComparison>]
type ActivityReference = private ActivityReference of scope: Guid * value: int64

[<RequireQualifiedAccess>]
module ActivityReference =
    let internal issue scope value = ActivityReference(scope, value)
    let scope (ActivityReference(scope, _)) = scope
    let value (ActivityReference(_, value)) = value

[<StructuralEquality; StructuralComparison>]
type ShapeReference =
    { Name: CanonicalName
      Version: int }

[<StructuralEquality; StructuralComparison>]
type FragmentReference =
    { Name: CanonicalName
      Version: int }

[<StructuralEquality; StructuralComparison>]
type OperationReference =
    { Name: CanonicalName }

[<StructuralEquality; StructuralComparison>]
type EventReference =
    { Name: CanonicalName }

type ScalarKind =
    | Boolean
    | Integer
    | Decimal
    | Text
    | Bytes

type ShapeValue =
    | UnitValue
    | BooleanValue of bool
    | IntegerValue of int64
    | DecimalValue of decimal
    | TextValue of string
    | BytesValue of byte array
    | RecordValue of fields: Map<string, ShapeValue> * fragments: Map<FragmentReference, ShapeValue>
    | SequenceValue of ShapeValue list
    | ChoiceValue of caseName: string * value: ShapeValue

type RecordField =
    { Name: string
      Shape: ShapeReference
      Required: bool }

type ShapeBody =
    | UnitShape
    | ScalarShape of ScalarKind
    | RecordShape of fields: RecordField list
    | SequenceShape of element: ShapeReference
    | ChoiceShape of cases: Map<string, ShapeReference>
    | OpaqueShape of mediaType: string

type ShapeDefinition =
    { Reference: ShapeReference
      Description: string
      Body: ShapeBody
      AcceptedFragments: Set<FragmentReference>
      IsOpenToFragments: bool }

type FragmentDefinition =
    { Reference: FragmentReference
      Description: string
      Shape: ShapeReference }

type ConstraintRequirement =
    { Constraint: ConstraintReference
      Parameters: ShapeValue }

type ConstraintDefinition =
    { Reference: ConstraintReference
      Name: CanonicalName
      ParameterShape: ShapeReference
      Description: string }

type OperationDefinition =
    { Reference: OperationReference
      Description: string
      Target: ActorReference
      CommandShape: ShapeReference
      ResultShape: ShapeReference
      Constraints: ConstraintRequirement list }

type Capability =
    { Reference: CapabilityReference
      Name: CanonicalName
      Holder: ActorReference
      Target: ActorReference
      Operations: Set<OperationReference>
      AddedConstraints: ConstraintRequirement list
      Parent: CapabilityReference option
      IssuedBy: ActorReference option
      DelegationAllowed: bool }

type Actor =
    { Reference: ActorReference
      Name: CanonicalName }

type ExecutionRequest =
    { Initiator: ActorReference
      Target: ActorReference
      PresentedCapability: CapabilityReference
      Operation: OperationReference
      Command: ShapeValue
      Occurrence: OccurrenceReference option
      Context: Map<string, string> }

[<StructuralEquality; StructuralComparison>]
type TimeDomainReference = private TimeDomainReference of CanonicalName

[<RequireQualifiedAccess>]
module TimeDomainReference =
    let create name = TimeDomainReference name
    let name (TimeDomainReference name) = name

type TemporalMark =
    { Milliseconds: int64
      TimeDomain: TimeDomainReference
      UncertaintyMilliseconds: int64 option }
type ExecutionStatus =
    | Succeeded
    | Denied
    | Failed

type ExecutionAudit =
    { Execution: ExecutionReference
      Initiator: ActorReference
      Target: ActorReference
      PresentedCapability: CapabilityReference
      Operation: OperationReference
      Status: ExecutionStatus
      Reason: string option
      Occurrence: OccurrenceReference option
      RecordedAt: TemporalMark }

type EventDefinition =
    { Reference: EventReference
      Description: string
      AssertionShape: ShapeReference }

type Event =
    { Reference: EventReference
      Occurrence: OccurrenceReference
      Emitter: ActorReference
      CausedBy: ExecutionReference
      Payload: ShapeValue
      EmittedAt: TemporalMark
      OccurredAt: TemporalMark option }

type ExecutionOutcome =
    { Event: Event
      Execution: ExecutionReference
      TerminalFor: ExecutionReference
      Operation: OperationReference
      Status: ExecutionStatus
      Result: ShapeValue option
      DetailsShape: ShapeReference option
      Details: ShapeValue option
      Reason: string option
      EmittedAt: TemporalMark }

type EventDraft =
    { Reference: EventReference
      Emitter: ActorReference
      Payload: ShapeValue
      OccurredAt: TemporalMark option }

type GenesisOccurrence =
    { Occurrence: OccurrenceReference
      Policy: CanonicalName
      IntroducedActors: ActorReference list
      IntroducedCapabilities: CapabilityReference list
      RecordedAt: TemporalMark }

type ProvenanceClaim =
    { Subject: string
      Predicate: CanonicalName
      Object: string
      CausedBy: ExecutionReference }

[<RequireQualifiedAccess>]
module BuiltIn =
    let private shape name version : ShapeReference =
        { Name = CanonicalName.create name
          Version = version }

    let unitShape = shape "brontide.base.unit" 1
    let booleanShape = shape "brontide.base.boolean" 1
    let integerShape = shape "brontide.base.integer" 1
    let decimalShape = shape "brontide.base.decimal" 1
    let textShape = shape "brontide.base.text" 1
    let bytesShape = shape "brontide.base.bytes" 1
    let executionOutcomeEvent: EventReference =
        { Name = CanonicalName.create "Brontide.Minimal:Execution.Outcome" }
