namespace Brontide.Minimal.Architecture07.TestConsole

open System
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel
open Brontide.Minimal.Experimental.Composition
open Brontide.Minimal.Experimental.PersistentInformation

module Program =
    let private name = CanonicalName.create
    let private node (value: string) =
        JsonValue.Create(value)
        |> Option.ofObj
        |> Option.map (fun item -> item :> JsonNode)
        |> Option.defaultWith (fun () -> invalidOp "A comparison string cannot produce a null JSON node.")
    let private intNode (value: int) = JsonValue.Create(value) :> JsonNode

    let private accepted id value =
        let result = JsonObject()
        result["id"] <- node id
        result["status"] <- node "accepted"
        result["value"] <- node value
        result["diagnostic"] <- node "none"
        result

    let private denied id diagnostic =
        let result = JsonObject()
        result["id"] <- node id
        result["status"] <- node "denied"
        result["diagnostic"] <- node diagnostic
        result

    let private diagnostic = function
        | ConstraintSatisfied -> "none"
        | ConstraintUnsatisfied -> "unsatisfied"
        | UnsupportedConstraint -> "unsupported-constraint"
        | InvalidConstraintValue -> "invalid-constraint-value"
        | ConstraintEvaluatorFailure -> "evaluator-failure"
        | InvalidConstraintExpression -> "invalid-constraint-expression"

    let private definitions () =
        let timeDomain = TimeDomainReference.create (name "Comparison.Time")
        let initial = World.create (Guid.NewGuid()) timeDomain
        let register world value : ConstraintDefinition * World =
            World.registerConstraint (name value) BuiltIn.textShape "comparison atom" world
            |> Result.defaultWith failwith
        let yes, world = register initial "Constraint.Yes"
        let no, world = register world "Constraint.No"
        let unknown, _ = register world "Constraint.Unknown"
        yes, no, unknown

    let private observeConstraint id scenario =
        let yes, no, unknown = definitions ()
        let atom (definition: ConstraintDefinition) value =
            AtomicConstraint { Constraint = definition.Reference; Parameters = TextValue value }
        let expression =
            match scenario with
            | "conjunction-satisfied" -> AllOf [ atom yes "yes"; atom yes "yes" ]
            | "unsupported-poisons-disjunction" -> AnyOf [ atom yes "yes"; atom unknown "unknown" ]
            | "unsatisfied" -> atom no "no"
            | value -> invalidOp $"Unknown constraint scenario '{value}'."
        let evaluate (requirement: ConstraintRequirement) =
            if requirement.Constraint = yes.Reference then ConstraintAtomEvaluation.satisfied
            elif requirement.Constraint = no.Reference then ConstraintAtomEvaluation.unsatisfied "not satisfied"
            else ConstraintAtomEvaluation.unsupported (name "Constraint.Unknown")
        let evaluation = ConstraintExpression.evaluate evaluate expression
        if evaluation.Outcome = Satisfied then accepted id "satisfied"
        else denied id (diagnostic evaluation.DiagnosticCategory)

    let private observeCanonicalName id text =
        match CanonicalMemberName.tryCreate text with
        | Ok canonical -> accepted id (CanonicalMemberName.value canonical)
        | Error _ -> denied id "name-invalid"

    let private bindingRegistry () =
        let timeDomain = TimeDomainReference.create (name "Comparison.BindingTime")
        let initial = World.create (Guid.NewGuid()) timeDomain
        let register world value : ConstraintDefinition * World =
            World.registerConstraint value BuiltIn.textShape "comparison attribute" world
            |> Result.defaultWith failwith
        let region = name "Attribute.Region"
        let exotic = name "Attribute.Exotic"
        let regionDefinition, world = register initial region
        let exoticDefinition, _ = register world exotic
        let attributes = Map.ofList [ regionDefinition.Reference, region; exoticDefinition.Reference, exotic ]
        let attributeOf reference = Map.tryFind reference attributes
        let atom (definition: ConstraintDefinition) value = AtomicConstraint { Constraint = definition.Reference; Parameters = TextValue value }
        attributeOf, region, exotic, atom regionDefinition, atom exoticDefinition

    let private sourced attribute value : AttributeValue =
        { Attribute = attribute
          SourceOperation = { Name = name "Operation.ReadAttribute" }
          VocabularyVersion = 1
          ResultShape = BuiltIn.textShape
          ResultPath = "/value"
          Value = TextValue value }

    let private candidate provider values : AttributeCandidate =
        { Provider = name provider
          Attributes = values |> List.map (fun (attribute, value) -> sourced attribute value) }

    let private disposition = function
        | Selected -> "selected"
        | AttributeCandidateDisposition.Unsatisfied -> "unsatisfied"
        | Unevaluatable -> "unevaluatable"

    let private observeBinding id scenario =
        let attributeOf, region, exotic, regionAtom, exoticAtom = bindingRegistry ()
        let expression, candidates =
            match scenario with
            | "ordinal-selection" ->
                regionAtom "north",
                [ candidate "Provider.B" [ region, "north" ]; candidate "Provider.A" [ region, "north" ] ]
            | "unsupported-then-selected" ->
                AllOf [ regionAtom "north"; exoticAtom "yes" ],
                [ candidate "Provider.A" [ region, "north" ]; candidate "Provider.B" [ region, "north"; exotic, "yes" ] ]
            | "restore-recorded-selection" ->
                regionAtom "north", [ candidate "Provider.A" [ region, "north" ] ]
            | value -> invalidOp $"Unknown binding scenario '{value}'."
        let resolved = AttributeConstrainedBinding.resolve attributeOf (name "Binding.Cooling") expression candidates
        match resolved.Binding with
        | None ->
            resolved.Provenance
            |> List.tryLast
            |> Option.map (fun outcome -> denied id (diagnostic outcome.DiagnosticCategory))
            |> Option.defaultValue (denied id "unsatisfied")
        | Some binding ->
            let result = accepted id (CanonicalName.value binding.SelectedProvider)
            let provenance = JsonArray()
            resolved.Provenance
            |> List.iter (fun outcome ->
                provenance.Add(node $"{CanonicalName.value outcome.Provider}:{disposition outcome.Disposition}"))
            result["provenance"] <- provenance
            if scenario = "restore-recorded-selection" then
                let restored = AttributeConstrainedBinding.restore attributeOf expression binding
                result["restoration"] <- node (CanonicalName.value restored.Binding.Value.SelectedProvider)
            result

    let private actor () =
        let timeDomain = TimeDomainReference.create (name "Comparison.ActorTime")
        let initial = World.create (Guid.NewGuid()) timeDomain
        let created, _ =
            World.genesis
                (name "Comparison.ActorPolicy")
                { Milliseconds = 0L; TimeDomain = timeDomain; UncertaintyMilliseconds = None }
                (fun genesis world -> Genesis.actor genesis (name "Comparison.Issuer") world)
                initial
            |> Result.defaultWith failwith
        created.Reference

    let private observePersistentInformation id scenario =
        let role = StoreRoleId.create "core"
        let roleDefinition =
            { Id = role; IdentityBearing = true; Required = true; AbsenceBehavior = DatasetUnavailable }
        match scenario with
        | "corpus-rejects-external-coordination" ->
            match OpaqueCorpus.create (CorpusId.create "settings") "1" (Some ExternalCoordination) [ roleDefinition ] with
            | Error failure -> denied id failure.Code
            | Ok _ -> accepted id "unexpected"
        | "router-rejects-unsupported-guarantee" ->
            let store = MemoryStore(StoreId.create "only", Set.singleton Durable)
            match RouterEndpoint.create (RouterId.create "router") (Set.singleton Encrypted) [ store ] true with
            | Error failure -> denied id failure.Code
            | Ok _ -> accepted id "unexpected"
        | "router-fallback"
        | "router-redacts-topology" ->
            let first = MemoryStore(StoreId.create "first", Set.singleton Durable)
            let second = MemoryStore(StoreId.create "second", Set.singleton Durable)
            first.IsAvailable <- (scenario = "router-redacts-topology")
            let router =
                RouterEndpoint.create (RouterId.create "router") (Set.singleton Durable) [ first; second ] false
                |> Result.defaultWith (fun failure -> failwith failure.Code)
            let result =
                if scenario = "router-fallback" then
                    router.Append "value" |> Result.defaultWith (fun failure -> failwith failure.Code) |> ignore
                    accepted id (if second.Read() = [ "value" ] then "second" else "unexpected")
                else
                    accepted id (if (router.Describe true).SelectedBacking.IsNone then "redacted" else "visible")
            let guarantees = JsonArray()
            guarantees.Add(node "durable")
            result["guarantees"] <- guarantees
            result
        | "dataset-concurrency-mismatch"
        | "dataset-identity-survives-content-loss" ->
            let corpus =
                OpaqueCorpus.create (CorpusId.create "settings") "1" (Some SingleWriter) [ roleDefinition ]
                |> Result.defaultWith (fun failure -> failwith failure.Code)
            let store = MemoryStore(StoreId.create "primary", Set.singleton Durable)
            let registry = DatasetRegistry()
            let dataset = DatasetId.create "dataset-1"
            let issuance = { Issuer = actor (); IssuingOperation = { Name = name "Dataset.Create" } }
            registry.Issue(issuance, corpus, dataset, Map.ofList [ role, store :> IStoreEndpoint ])
            |> Result.defaultWith (fun failure -> failwith failure.Code)
            |> ignore
            if scenario = "dataset-concurrency-mismatch" then
                let failure =
                    registry.Append(dataset, role, ExternalCoordination, "value")
                    |> function Error value -> value | Ok _ -> failwith "Expected concurrency refusal."
                let result = denied id failure.Code
                result["effects"] <- intNode store.AppendCount
                result
            else
                registry.Append(dataset, role, SingleWriter, "value") |> ignore
                store.Clear()
                let result = accepted id (registry.Datasets.Head.Id |> DatasetId.value)
                let empty = registry.Read(dataset, role, SingleWriter) |> Result.map List.isEmpty |> Result.defaultValue false
                result["restoration"] <- node (if empty then "empty-content" else "content-present")
                result
        | value -> invalidOp $"Unknown persistent-information scenario '{value}'."

    let private requiredString (element: JsonElement) (property: string) =
        element.GetProperty(property).GetString()
        |> Option.ofObj
        |> Option.defaultWith (fun () -> invalidOp $"Comparison property '{property}' cannot be null.")

    let private observe (vector: JsonElement) =
        let id = requiredString vector "id"
        let operation = requiredString vector "operation"
        let input = vector.GetProperty("input")
        match operation with
        | "constraint" -> observeConstraint id (requiredString input "scenario")
        | "canonical-name" -> observeCanonicalName id (requiredString input "text")
        | "attribute-binding" -> observeBinding id (requiredString input "scenario")
        | "persistent-information" -> observePersistentInformation id (requiredString input "scenario")
        | value -> invalidOp $"Unknown comparison operation '{value}'."

    [<EntryPoint>]
    let main args =
        if args.Length <> 2 then
            eprintfn "Usage: architecture07 <fixture.json> <observations.json>"
            2
        else
            try
                use fixture = JsonDocument.Parse(File.ReadAllText args[0])
                let observations = JsonArray()
                fixture.RootElement.GetProperty("vectors").EnumerateArray()
                |> Seq.iter (fun vector -> observations.Add(observe vector))
                File.WriteAllText(args[1], observations.ToJsonString(JsonSerializerOptions(WriteIndented = true)))
                0
            with error ->
                eprintfn "%s" error.Message
                1
