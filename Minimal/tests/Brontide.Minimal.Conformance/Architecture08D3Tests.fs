namespace Brontide.Minimal.Conformance

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

module private D3Helpers =
    let name value = CanonicalName.create value

    let get = function
        | Ok value -> value
        | Error message -> failwith message

    let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:Draft08.D3.Clock")

    let mark milliseconds =
        { Milliseconds = milliseconds
          TimeDomain = timeDomain
          UncertaintyMilliseconds = None }

    let shape value version : ShapeReference =
        { Name = name value
          Version = version }

    let areaV1 = shape "Example:Area" 1
    let areaV2 = shape "Example:Area" 2
    let payloadV1 = shape "Example:Payload" 1
    let payloadV2 = shape "Example:Payload" 2
    let newName = name "Example:AreaPolicy.V2"
    let oldName = name "Example:AreaPolicy.V1"
    let declinedName = name "Example:DeclinedPolicy"
    let operation : OperationReference = { Name = name "Example:ExecuteD3" }

    let declaration constraintName valueShape semantics : ConstraintDeclaration =
        { Name = constraintName
          Version = 1
          ValueShape = valueShape
          EvaluationSemantics = semantics
          EvaluatorDomain = ConstraintEvaluatorDomain.TargetAuthority
          UnknownBehavior = ConstraintUnknownBehavior.Deny
          AccountingScope = ConstraintAccountingScope.NotQuantified
          EvolutionPolicy = ConstraintEvolutionPolicy.ParallelCanonicalName }

    let recordShape reference fields : ShapeDefinition =
        { Reference = reference
          Description = "A08-D3 test Shape"
          Body = RecordShape fields
          AcceptedFragments = Set.empty
          IsOpenToFragments = true }

    let required fieldName fieldShape =
        { Name = fieldName
          Shape = fieldShape
          Required = true }

    let optional fieldName fieldShape =
        { Name = fieldName
          Shape = fieldShape
          Required = false }

    type Fixture =
        { World: World
          Holder: Actor
          Target: Actor
          Root: Capability
          NewConstraint: ConstraintDefinition
          OldConstraint: ConstraintDefinition
          DeclinedConstraint: ConstraintDefinition }

    let prepare commandShape expressionBuilder =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let created, ready =
            World.genesis
                (name "Brontide.Minimal.Tests:Draft08.D3.Policy")
                (mark 0L)
                (fun genesis world ->
                    let world =
                        world
                        |> World.registerShape (recordShape areaV1 [ required "region" BuiltIn.textShape ])
                        |> get
                        |> World.registerShape
                            (recordShape areaV2
                                [ required "region" BuiltIn.textShape
                                  optional "exclusions" BuiltIn.textShape ])
                        |> get
                        |> World.registerShape (recordShape payloadV1 [ required "command" BuiltIn.textShape ])
                        |> get
                        |> World.registerShape
                            (recordShape payloadV2
                                [ required "command" BuiltIn.textShape
                                  optional "optional-note" BuiltIn.textShape ])
                        |> get
                    let holder, world = Genesis.actor genesis (name "Example:Holder") world
                    let target, world = Genesis.actor genesis (name "Example:Target") world
                    let newConstraint, world =
                        World.registerConstraintDeclaration
                            (declaration newName areaV1 "new region policy")
                            world
                        |> get
                    let oldConstraint, world =
                        World.registerConstraintDeclaration
                            (declaration oldName areaV1 "old fallback policy")
                            world
                        |> get
                    let declinedConstraint, world =
                        World.registerConstraintDeclaration
                            (declaration declinedName BuiltIn.textShape "declined policy")
                            world
                        |> get
                    let operationDefinition =
                        { Reference = operation
                          Description = "A08-D3 checked effect"
                          Target = target.Reference
                          CommandShape = commandShape
                          ResultShape = BuiltIn.unitShape
                          Constraints = [] }
                    let world = World.registerOperation operationDefinition world |> get
                    let root, world =
                        Genesis.capabilityWithExpressions
                            genesis
                            (name "Example:D3.Root")
                            holder.Reference
                            target.Reference
                            (Set.singleton operation)
                            (expressionBuilder newConstraint oldConstraint declinedConstraint)
                            world
                        |> get
                    (holder, target, root, newConstraint, oldConstraint, declinedConstraint), world)
                initial
            |> get
        let holder, target, root, newConstraint, oldConstraint, declinedConstraint = created
        { World = ready
          Holder = holder
          Target = target
          Root = root
          NewConstraint = newConstraint
          OldConstraint = oldConstraint
          DeclinedConstraint = declinedConstraint }

    let areaValue includeExclusions =
        let fields =
            if includeExclusions then
                Map.ofList
                    [ "region", TextValue "north"
                      "exclusions", TextValue "restricted" ]
            else
                Map.ofList [ "region", TextValue "north" ]
        RecordValue(fields, Map.empty)

    let request fixture presentedShape command : Draft08ExecutionRequest =
        { Request =
            { Initiator = fixture.Holder.Reference
              Target = fixture.Target.Reference
              PresentedCapability = fixture.Root.Reference
              Operation = operation
              Command = command
              Occurrence = None
              Context = Map.empty }
          RequestedOrigin = OriginClass.Unverified
          PresentedCommandShape = presentedShape }

    let environment evaluators handler =
        { TrustedTime = mark 1L
          ConstraintEvaluators = evaluators
          Handlers = Map.ofList [ operation, handler ] }

open D3Helpers

[<TestFixture>]
type Architecture08D3Tests() =
    [<Test>]
    member _.``D3_C1 BR_08_ADV_C9_001 declined declaration is named and denies before effect`` () =
        let fixture =
            prepare BuiltIn.unitShape (fun _ _ declined ->
                [ AtomicConstraint
                    { Constraint = declined.Reference
                      ParameterShape = BuiltIn.textShape
                      Parameters = TextValue "sensitive-policy-value" } ])
        let mutable effects = 0
        let result =
            World.stepDraft08
                (environment Map.empty (fun _ -> effects <- effects + 1; Ok(UnitValue, [], [])))
                fixture.World
                (request fixture BuiltIn.unitShape UnitValue)

        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Denied)
            Assert.That(result.Outcome.Reason.Value, Does.Contain(CanonicalName.value declinedName))
            Assert.That(result.Outcome.Reason.Value, Does.Not.Contain "sensitive-policy-value")
            Assert.That(effects, Is.Zero)))

    [<Test>]
    member _.``D3_C2 BR_08_ADV_C9_002 changed semantics under one name is rejected`` () =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let mutable secondRegistration = None
        let completed =
            World.genesis
                (name "Example:ImmutableDeclarations")
                (mark 0L)
                (fun _ world ->
                    let world =
                        world
                        |> World.registerShape (recordShape areaV1 [ required "region" BuiltIn.textShape ])
                        |> get
                        |> World.registerShape
                            (recordShape areaV2
                                [ required "region" BuiltIn.textShape
                                  optional "exclusions" BuiltIn.textShape ])
                        |> get
                    let _, world =
                        World.registerConstraintDeclaration
                            (declaration newName areaV1 "region must be admitted")
                            world
                        |> get
                    match
                        World.registerConstraintDeclaration
                            (declaration newName areaV2 "region and exclusions must be admitted")
                            world
                    with
                    | Ok(_, changedWorld) ->
                        secondRegistration <- Some(Ok())
                        (), changedWorld
                    | Error message ->
                        secondRegistration <- Some(Error message)
                        (), world)
                initial

        completed |> get |> ignore
        match secondRegistration with
        | Some(Ok()) -> Assert.Fail("A changed declaration under the same canonical name was accepted.")
        | Some(Error message) -> Assert.That(message, Does.Contain "new canonical name")
        | None -> Assert.Fail("The second declaration was not checked.")

    [<Test>]
    member _.``D3_C3 BR_08_ADV_C9_003 recognition set is complete ordered and effect free`` () =
        let fixture = prepare BuiltIn.unitShape (fun _ _ _ -> [])
        let mutable evaluatorCalls = 0
        let evaluator _ _ = evaluatorCalls <- evaluatorCalls + 1; Ok()
        let env = environment (Map.ofList [ fixture.OldConstraint.Reference, evaluator ]) (fun _ -> Ok(UnitValue, [], []))
        let first = World.constraintRecognitionSet env fixture.World
        let second = World.constraintRecognitionSet env fixture.World

        Assert.Multiple(Action(fun () ->
            Assert.That((second = first), Is.True)
            Assert.That(first |> List.map (fun item -> item.Declaration.Name), Is.Ordered)
            Assert.That(
                first |> List.find (fun item -> item.Declaration.Name = declinedName) |> _.Decision,
                Is.EqualTo ConstraintRecognitionDecision.Declined)
            Assert.That(
                first |> List.find (fun item -> item.Declaration.Name = oldName) |> _.Decision,
                Is.EqualTo ConstraintRecognitionDecision.Implemented)
            Assert.That(
                first |> List.exists (fun item -> item.Declaration.Name = BuiltIn.delegationDepthConstraintName),
                Is.True)
            Assert.That(evaluatorCalls, Is.Zero)))

    [<Test>]
    member _.``D3_C4 BR_08_ADV_C8_001 Constraint value version is not projected`` () =
        let fixture =
            prepare BuiltIn.unitShape (fun newer _ _ ->
                [ AtomicConstraint
                    { Constraint = newer.Reference
                      ParameterShape = areaV2
                      Parameters = areaValue true } ])
        let mutable evaluatorCalls = 0
        let mutable effects = 0
        let evaluator _ _ = evaluatorCalls <- evaluatorCalls + 1; Ok()
        let env =
            environment
                (Map.ofList [ fixture.NewConstraint.Reference, evaluator ])
                (fun _ -> effects <- effects + 1; Ok(UnitValue, [], []))
        let result = World.stepDraft08 env fixture.World (request fixture BuiltIn.unitShape UnitValue)

        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Denied)
            Assert.That(evaluatorCalls, Is.Zero)
            Assert.That(effects, Is.Zero)))

    [<Test>]
    member _.``D3_C5 BR_08_ADV_C8_002 authored old Constraint fallback authorises`` () =
        let fixture =
            prepare BuiltIn.unitShape (fun newer older _ ->
                [ AnyOf
                    [ AtomicConstraint
                        { Constraint = newer.Reference
                          ParameterShape = areaV2
                          Parameters = areaValue true }
                      AtomicConstraint
                        { Constraint = older.Reference
                          ParameterShape = areaV1
                          Parameters = areaValue false } ] ])
        let mutable newCalls = 0
        let mutable oldCalls = 0
        let mutable effects = 0
        let env =
            environment
                (Map.ofList
                    [ fixture.NewConstraint.Reference, (fun _ _ -> newCalls <- newCalls + 1; Ok())
                      fixture.OldConstraint.Reference, (fun _ _ -> oldCalls <- oldCalls + 1; Ok()) ])
                (fun _ -> effects <- effects + 1; Ok(UnitValue, [], []))
        let result = World.stepDraft08 env fixture.World (request fixture BuiltIn.unitShape UnitValue)

        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(newCalls, Is.Zero)
            Assert.That(oldCalls, Is.EqualTo 1)
            Assert.That(effects, Is.EqualTo 1)))

    [<Test>]
    member _.``D3_C6 BR_08_ADV_C8_003 payload projection remains additive`` () =
        let fixture = prepare payloadV1 (fun _ _ _ -> [])
        let mutable delivered = UnitValue
        let env =
            environment Map.empty (fun execution -> delivered <- execution.Command; Ok(UnitValue, [], []))
        let command =
            RecordValue(
                Map.ofList
                    [ "command", TextValue "run"
                      "optional-note", TextValue "ignored" ],
                Map.empty)
        let result = World.stepDraft08 env fixture.World (request fixture payloadV2 command)

        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Succeeded)
            match delivered with
            | RecordValue(fields, _) -> Assert.That((fields |> Map.toList |> List.map fst) = [ "command" ], Is.True)
            | _ -> Assert.Fail("The handler did not receive the projected record payload.")))
