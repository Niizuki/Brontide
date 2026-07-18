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

        if String.IsNullOrWhiteSpace value then
            Error "A canonical name cannot be empty."
        elif value.StartsWith('.') || value.EndsWith('.') then
            Error "A canonical name cannot start or end with a dot."
        elif
            value.Split('.')
            |> Array.exists (fun segment ->
                String.IsNullOrWhiteSpace segment
                || not (segment |> Seq.forall validCharacter))
        then
            Error "A canonical name contains an empty or invalid segment."
        else
            Ok(CanonicalName value)

    let create value =
        match tryCreate value with
        | Ok name -> name
        | Error message -> invalidArg (nameof value) message

[<Struct; StructuralEquality; StructuralComparison>]
type ActorReference = { Scope: Guid; Value: int64 }

[<Struct; StructuralEquality; StructuralComparison>]
type CapabilityReference = { Scope: Guid; Value: int64 }

[<Struct; StructuralEquality; StructuralComparison>]
type ConstraintReference = { Scope: Guid; Value: int64 }

[<Struct; StructuralEquality; StructuralComparison>]
type ExecutionReference = { Scope: Guid; Value: int64 }

[<Struct; StructuralEquality; StructuralComparison>]
type OccurrenceReference = { Scope: Guid; Value: int64 }

[<Struct; StructuralEquality; StructuralComparison>]
type ActivityReference = { Scope: Guid; Value: int64 }

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
    { Name: CanonicalName
      Version: int }

[<StructuralEquality; StructuralComparison>]
type EventReference =
    { Name: CanonicalName
      Version: int }

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
      CommandShape: ShapeReference
      ResultShape: ShapeReference
      Constraints: ConstraintRequirement list }

type Capability =
    { Reference: CapabilityReference
      Name: CanonicalName
      Operations: Set<OperationReference>
      Parent: CapabilityReference option }

type Actor =
    { Reference: ActorReference
      Name: CanonicalName }

type ExecutionRequest =
    { Actor: ActorReference
      Operation: OperationReference
      Command: ShapeValue
      Occurrence: OccurrenceReference option
      Context: Map<string, string> }

type ExecutionStatus =
    | Succeeded
    | Denied
    | Failed

type ExecutionOutcome =
    { Execution: ExecutionReference
      Operation: OperationReference
      Status: ExecutionStatus
      Result: ShapeValue option
      Reason: string option }

type Event =
    { Reference: EventReference
      Occurrence: OccurrenceReference
      CausedBy: ExecutionReference
      Payload: ShapeValue }

type EventDraft =
    { Reference: EventReference
      Payload: ShapeValue }

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
