namespace Brontide.Minimal.Binding.Portable

open Brontide.Minimal.Model

/// Builders for fixture data whose references are known-good at authoring time.
///
/// A malformed reference here would be a defect in the fixture rather than a condition a peer can
/// present, so it is not part of the portable failure model.
module PortableFixtureSupport =

    let private force what result =
        match result with
        | Ok value -> value
        | Error _ -> invalidOp $"The fixture declares '{what}', which is outside the portable profile."

    let componentRef name version =
        PortableComponentRef.tryCreate name version |> force name

    let providerRef name version =
        PortableProviderRef.tryCreate name version |> force name

    let operationRef name version =
        PortableOperationRef.tryCreate name version |> force name

    let shapeRef name version =
        PortableShapeRef.tryCreate name version |> force name

    let fragmentRef name version =
        PortableFragmentRef.tryCreate name version |> force name

    let dependencyRef name version =
        PortableDependencyRef.tryCreate name version |> force name

    let required name shape : FieldDeclaration =
        { Name = name; Shape = shape; Required = true }

    let optional name shape : FieldDeclaration =
        { Name = name; Shape = shape; Required = false }

    let alternative name shape : AlternativeDeclaration = { Name = name; Shape = shape }

    let features =
        Map.ofList
            [ "establishment", true
              "readiness-signal", true
              "single-invocation", true
              "clean-withdrawal", true
              "clean-termination", true
              "retry", false
              "cancellation", false
              "streaming", false
              "ordering-guarantee", false
              "exactly-once-execution", false ]

    let crossTrustAuthority =
        { PresentationMode = AuthorityMode.CrossTrustNoCapabilityTransfer
          TrustBoundaryCrossed = true
          NoCapabilityTransfer = true
          ConstraintPolicy = ContractDocument.OnlyPermittedConstraintPolicy }

    let lifecycle =
        { ReplayProtectionDeclared = true
          ReplayWindow = "binding"
          Features = features }

/// The Cooling experiment restated as a fixture over the reusable portable layer.
///
/// Cooling is now a consumer of the contract rather than its definition: everything below is data
/// and a handler, and nothing in the reusable layer knows a Cooling rule. The declarations mirror
/// the neutral fixture under binding/portable/vectors/, which is what lets the neutral vectors be
/// executed against this stack without either side importing the other.
[<RequireQualifiedAccess>]
module CoolingFixture =

    open PortableFixtureSupport

    let component' = componentRef "interchange.tests.cooling-component" 1
    let provider = providerRef "interchange.tests.fixture-provider" 1
    let setEnabled = operationRef "interchange.tests.cooling.set-enabled" 1

    let commandV1 = shapeRef "interchange.tests.cooling.command" 1
    let commandV2 = shapeRef "interchange.tests.cooling.command" 2
    let commandV3 = shapeRef "interchange.tests.cooling.command" 3
    let result = shapeRef "interchange.tests.cooling.result" 1
    let details = shapeRef "interchange.tests.cooling.details" 1

    let encodingTags = shapeRef "interchange.tests.encoding.tags" 1
    let encodingTextSequence = shapeRef "interchange.tests.encoding.text-sequence" 1
    let encodingChoice = shapeRef "interchange.tests.encoding.choice" 1
    let encodingIntegers = shapeRef "interchange.tests.encoding.integers" 1
    let encodingScalars = shapeRef "interchange.tests.encoding.scalars" 1

    let hostContext = fragmentRef "interchange.tests.cooling.host-context" 1

    /// Declared by the contract but not by the negotiated Operation, which is what the closed
    /// fragment policy refuses rather than projects away.
    let note = fragmentRef "interchange.tests.cooling.note" 1

    let private text = PortableBuiltInShapes.text
    let private boolean = PortableBuiltInShapes.boolean
    let private signed = PortableBuiltInShapes.signed64

    let contract =
        { ContractVersion = ContractDocument.SupportedContractVersion
          Component = component'
          Provider = provider
          Provisions =
            [ { Kind = DependencyKind.Operation
                Reference = dependencyRef "interchange.tests.cooling.set-enabled" 1
                ProviderSpecific = false }
              { Kind = DependencyKind.Profile
                Reference = dependencyRef "interchange.tests.cooling-profile" 1
                ProviderSpecific = false }
              { Kind = DependencyKind.Binding
                Reference = dependencyRef "interchange.tests.portable-cbor-core" 1
                ProviderSpecific = true }
              { Kind = DependencyKind.ResourceFlavor
                Reference = dependencyRef "interchange.tests.copied-immutable-blob" 1
                ProviderSpecific = false } ]
          Requirements =
            [ { Kind = DependencyKind.Profile
                Reference = dependencyRef "interchange.tests.cooling-profile" 1
                Strength = RequirementStrength.Required
                ProviderSpecific = false }
              { Kind = DependencyKind.Binding
                Reference = dependencyRef "interchange.tests.portable-cbor-core" 1
                Strength = RequirementStrength.Required
                ProviderSpecific = true }
              { Kind = DependencyKind.Feature
                Reference = dependencyRef "interchange.tests.streaming" 1
                Strength = RequirementStrength.Opposed
                ProviderSpecific = false } ]
          Operations =
            [ { Reference = setEnabled
                InputShape = commandV1
                ResultShape = result
                DetailShape = details
                RequiredFragments = [ hostContext ]
                ResourceFlavors = [ ResourceFlavor.CopiedImmutableBlobToken ] } ]
          Shapes =
            [ { Reference = commandV1
                Body =
                  RecordBody(
                      FragmentPolicy.Open,
                      [ required "loop" text; required "enabled" boolean; optional "failureMode" text ]
                  ) }
              { Reference = commandV2
                Body =
                  RecordBody(
                      FragmentPolicy.Open,
                      [ required "loop" text
                        required "enabled" boolean
                        optional "failureMode" text
                        optional "requestedBy" text ]
                  ) }
              { Reference = commandV3
                Body =
                  RecordBody(
                      FragmentPolicy.Open,
                      [ required "loop" text; required "enabled" boolean; required "reason" text ]
                  ) }
              { Reference = result
                Body =
                  RecordBody(
                      FragmentPolicy.Closed,
                      [ required "loop" text
                        required "coolingEnabled" boolean
                        required "revision" signed
                        required "providerEffectCount" signed ]
                  ) }
              { Reference = details
                Body = RecordBody(FragmentPolicy.Closed, [ required "code" text; required "message" text ]) }
              { Reference = encodingTags
                Body = RecordBody(FragmentPolicy.Closed, [ required "tags" encodingTextSequence ]) }
              { Reference = encodingTextSequence
                Body = SequenceBody text }
              { Reference = encodingChoice
                Body = ChoiceBody [ alternative "text" text; alternative "count" signed ] }
              { Reference = encodingIntegers
                Body =
                  RecordBody(
                      FragmentPolicy.Closed,
                      [ for index in 0..9 -> required $"i%02d{index}" signed ]
                  ) }
              { Reference = encodingScalars
                Body =
                  RecordBody(
                      FragmentPolicy.Closed,
                      [ required "b" PortableBuiltInShapes.bytes
                        required "d" PortableBuiltInShapes.decimal
                        required "u" PortableBuiltInShapes.unit ]
                  ) } ]
          Fragments =
            [ { Reference = hostContext
                HostShape = commandV1
                Fields = [ required "requesterLabel" text ] }
              { Reference = note
                HostShape = result
                Fields = [ required "note" text ] } ]
          Authority = crossTrustAuthority
          Representation =
            { Representation = PortableRepresentations.PortableCborCore
              Framing = PortableRepresentations.LengthDelimited
              ResourceFlavors = [ ResourceFlavor.CopiedImmutableBlobToken ]
              AcceptedResourceHandles = [] }
          Limits = PortableLimits.declared
          Lifecycle = lifecycle }

    /// The same contract with the streaming feature offered as a provision, which the fixture's
    /// opposed requirement must refuse rather than ignore.
    let withStreamingProvision () =
        { contract with
            Provisions =
                contract.Provisions
                @ [ { Kind = DependencyKind.Feature
                      Reference = dependencyRef "interchange.tests.streaming" 1
                      ProviderSpecific = false } ] }

    /// The contract with the required cooling profile withdrawn from the provider's provisions.
    let withoutProfileProvision () =
        { contract with
            Provisions =
                contract.Provisions
                |> List.filter (fun provision ->
                    provision.Reference <> dependencyRef "interchange.tests.cooling-profile" 1) }

    let command (loop: string) (enabled: bool) failureMode requesterLabel requestedBy =
        let withOptional name value record =
            match value with
            | Some value -> PortableRecord.withField name (PortableText value) record
            | None -> record

        PortableRecord.ofFields [ "loop", PortableText loop; "enabled", PortableBoolean enabled ]
        |> withOptional "failureMode" failureMode
        |> withOptional "requestedBy" requestedBy
        |> fun record ->
            match requesterLabel with
            | Some label -> PortableRecord.withFragment hostContext [ "requesterLabel", PortableText label ] record
            | None -> record

    /// The ordinary authorized command: an attributable requester label and no failure mode.
    let authorizedCommand loop enabled =
        command loop enabled None (Some "operator") None

/// The Cooling provider domain.
///
/// The handler is the provider's own domain: for a cross-trust presentation it receives only
/// attributable context and exact addressing and decides for itself, reporting a refusal as a
/// shaped failed Outcome rather than as a protocol rejection.
type CoolingHandler() =
    let mutable revision = 0L
    let mutable effects = 0L

    member _.ProviderEffectCount = effects

    interface IPortableOperationHandler with
        member _.Invoke(_, input, _) =
            portable {
                // The command crosses into the stack's own model here, which is the point of the
                // adapter: the domain logic below never sees a portable type.
                let! native = PortableModelAdapter.toModel input

                let fields =
                    match native with
                    | RecordValue(fields, _) -> fields
                    | _ -> Map.empty

                let! loop =
                    match Map.tryFind "loop" fields with
                    | Some(TextValue loop) -> Ok loop
                    | _ -> invalidPayload "cooling-loop" "A Cooling command names its loop."

                let! enabled =
                    match Map.tryFind "enabled" fields with
                    | Some(BooleanValue enabled) -> Ok enabled
                    | _ -> invalidPayload "cooling-enabled" "A Cooling command declares whether cooling is enabled."

                match Map.tryFind "failureMode" fields with
                | Some(TextValue failureMode) ->
                    return
                        EffectFailed(
                            PortableRecord.ofFields
                                [ "code", PortableText failureMode
                                  "message", PortableText $"The cooling loop '{loop}' refused the command." ],
                            0L
                        )
                | _ ->
                    revision <- revision + 1L
                    effects <- effects + 1L

                    return
                        EffectSucceeded(
                            PortableRecord.ofFields
                                [ "loop", PortableText loop
                                  "coolingEnabled", PortableBoolean enabled
                                  "revision", PortableInteger revision
                                  "providerEffectCount", PortableInteger effects ],
                            effects
                        )
            }

/// The Catalog experiment restated over the same reusable layer.
///
/// Catalog contributes what Cooling cannot: more than one Operation, nested and repeated values,
/// and the addressing-only resource handle whose accept list the Binding Plan freezes.
[<RequireQualifiedAccess>]
module CatalogFixture =

    open PortableFixtureSupport

    let component' = componentRef "interchange.tests.catalog-component" 1
    let provider = providerRef "interchange.tests.catalog-provider" 1
    let upsert = operationRef "interchange.tests.catalog.upsert-items" 1
    let find = operationRef "interchange.tests.catalog.find-items" 1

    let item = shapeRef "interchange.tests.catalog.item" 1
    let itemSequence = shapeRef "interchange.tests.catalog.item-sequence" 1
    let textSequence = shapeRef "interchange.tests.catalog.text-sequence" 1
    let upsertCommand = shapeRef "interchange.tests.catalog.upsert-command" 1
    let upsertResult = shapeRef "interchange.tests.catalog.upsert-result" 1
    let findCommand = shapeRef "interchange.tests.catalog.find-command" 1
    let findResult = shapeRef "interchange.tests.catalog.find-result" 1
    let details = shapeRef "interchange.tests.catalog.details" 1

    /// The one handle the Binding Plan accepts. A handle outside this list is refused before any
    /// provider effect, and the refusal is a payload decision because a handle carries no authority.
    let acceptedHandle = "catalog-provider/primary"

    let private text = PortableBuiltInShapes.text
    let private signed = PortableBuiltInShapes.signed64

    let contract =
        { ContractVersion = ContractDocument.SupportedContractVersion
          Component = component'
          Provider = provider
          Provisions =
            [ { Kind = DependencyKind.Operation
                Reference = dependencyRef "interchange.tests.catalog.upsert-items" 1
                ProviderSpecific = false }
              { Kind = DependencyKind.Operation
                Reference = dependencyRef "interchange.tests.catalog.find-items" 1
                ProviderSpecific = false }
              { Kind = DependencyKind.ResourceFlavor
                Reference = dependencyRef "interchange.tests.addressing-only-handle" 1
                ProviderSpecific = false } ]
          Requirements =
            [ { Kind = DependencyKind.ResourceFlavor
                Reference = dependencyRef "interchange.tests.addressing-only-handle" 1
                Strength = RequirementStrength.Required
                ProviderSpecific = false } ]
          Operations =
            [ { Reference = upsert
                InputShape = upsertCommand
                ResultShape = upsertResult
                DetailShape = details
                RequiredFragments = []
                ResourceFlavors = [ ResourceFlavor.AddressingOnlyHandleToken ] }
              { Reference = find
                InputShape = findCommand
                ResultShape = findResult
                DetailShape = details
                RequiredFragments = []
                ResourceFlavors = [ ResourceFlavor.AddressingOnlyHandleToken ] } ]
          Shapes =
            [ { Reference = item
                Body =
                  RecordBody(
                      FragmentPolicy.Closed,
                      [ required "id" text; required "title" text; required "tags" textSequence ]
                  ) }
              { Reference = itemSequence; Body = SequenceBody item }
              { Reference = textSequence; Body = SequenceBody text }
              { Reference = upsertCommand
                Body = RecordBody(FragmentPolicy.Closed, [ required "items" itemSequence ]) }
              { Reference = upsertResult
                Body = RecordBody(FragmentPolicy.Closed, [ required "stored" signed ]) }
              { Reference = findCommand
                Body = RecordBody(FragmentPolicy.Closed, [ required "ids" textSequence ]) }
              { Reference = findResult
                Body = RecordBody(FragmentPolicy.Closed, [ required "items" itemSequence ]) }
              { Reference = details
                Body = RecordBody(FragmentPolicy.Closed, [ required "code" text; required "message" text ]) } ]
          Fragments = []
          Authority = crossTrustAuthority
          Representation =
            { Representation = PortableRepresentations.PortableCborCore
              Framing = PortableRepresentations.LengthDelimited
              ResourceFlavors = [ ResourceFlavor.AddressingOnlyHandleToken ]
              AcceptedResourceHandles = [ acceptedHandle ] }
          Limits = PortableLimits.declared
          Lifecycle = lifecycle }

    let itemValue id title (tags: string list) =
        PortableRecord.ofFields
            [ "id", PortableText id
              "title", PortableText title
              "tags", PortableSequence(tags |> List.map PortableText) ]

    let upsertCommandValue items =
        PortableRecord.ofFields [ "items", PortableSequence items ]

    let findCommandValue (ids: string list) =
        PortableRecord.ofFields [ "ids", PortableSequence(ids |> List.map PortableText) ]

    let handle provider id = AddressingHandle("catalog", provider, id)

/// The Catalog provider domain: one session's state, addressed by handles it retains.
type CatalogHandler() =
    let mutable stored: Map<string, PortableValue> = Map.empty
    let mutable effects = 0L

    member _.ProviderEffectCount = effects

    interface IPortableOperationHandler with
        member _.Invoke(operation, input, _) =
            let fields =
                match input with
                | PortableRecord(fields, _) -> fields
                | _ -> Map.empty

            if operation = CatalogFixture.upsert then
                match Map.tryFind "items" fields with
                | Some(PortableSequence items) ->
                    for candidate in items do
                        match PortableRecord.tryField "id" candidate with
                        | Some(PortableText id) -> stored <- Map.add id candidate stored
                        | _ -> ()

                    effects <- effects + 1L

                    Ok(
                        EffectSucceeded(
                            PortableRecord.ofFields [ "stored", PortableInteger(int64 (Map.count stored)) ],
                            effects
                        )
                    )
                | _ -> invalidPayload "catalog-items" "An upsert command carries a sequence of items."
            else
                match Map.tryFind "ids" fields with
                | Some(PortableSequence ids) ->
                    let found =
                        ids
                        |> List.choose (fun id ->
                            match id with
                            | PortableText id -> Map.tryFind id stored
                            | _ -> None)

                    if List.isEmpty found then
                        // A domain refusal is a shaped failed Outcome, never a protocol rejection.
                        Ok(
                            EffectFailed(
                                PortableRecord.ofFields
                                    [ "code", PortableText "not-found"
                                      "message", PortableText "No requested item is present in this session." ],
                                0L
                            )
                        )
                    else
                        effects <- effects + 1L
                        Ok(EffectSucceeded(PortableRecord.ofFields [ "items", PortableSequence found ], effects))
                | _ -> invalidPayload "catalog-ids" "A find command carries a sequence of identifiers."
