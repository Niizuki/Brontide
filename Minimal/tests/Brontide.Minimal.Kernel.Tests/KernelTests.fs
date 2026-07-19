namespace Brontide.Minimal.Kernel.Tests

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel
open Brontide.Minimal.Extensions.Events
open Brontide.Minimal.Extensions.Flow
open Brontide.Minimal.Vocabularies.Cooling

[<TestFixture>]
type NativeSemanticsTests() =
    let name value = CanonicalName.create value

    let issuedExecution () =
        let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:FlowClock")
        let operation: OperationReference = { Name = name "Brontide.Minimal.Tests:Flow.ExecutionSource" }
        let initial = World.create (Guid.NewGuid()) timeDomain

        let (holder, target, capability), ready =
            World.genesis
                (name "Brontide.Minimal.Tests:FlowPolicy")
                { Milliseconds = 0L; TimeDomain = timeDomain; UncertaintyMilliseconds = None }
                (fun genesis world ->
                    let holder, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:FlowHolder") world
                    let target, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:FlowTarget") world
                    let world =
                        World.registerOperation
                            { Reference = operation
                              Description = "Issue an opaque Execution reference for Flow tests."
                              Target = target.Reference
                              CommandShape = BuiltIn.unitShape
                              ResultShape = BuiltIn.unitShape
                              Constraints = [] }
                            world
                        |> Result.defaultWith failwith

                    let capability, world =
                        Genesis.capability
                            genesis
                            (name "Brontide.Minimal.Tests:FlowGrant")
                            holder.Reference
                            target.Reference
                            (Set.singleton operation)
                            []
                            false
                            world
                        |> Result.defaultWith failwith

                    ((holder, target, capability), world))
                initial
            |> Result.defaultWith failwith

        World.step
            { TrustedTime =
                { Milliseconds = 1L
                  TimeDomain = timeDomain
                  UncertaintyMilliseconds = None }
              ConstraintEvaluators = Map.empty
              Handlers = Map.ofList [ operation, fun _ -> Ok(UnitValue, [], []) ] }
            ready
            { Initiator = holder.Reference
              Target = target.Reference
              PresentedCapability = capability.Reference
              Operation = operation
              Command = UnitValue
              Occurrence = None
              Context = Map.empty }
        |> _.Outcome.Execution

    [<Test>]
    member _.``BR_07_CONSTRAINT_001 unknown atoms poison all composite positions`` () =
        let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:ConstraintClock")
        let initial = World.create (Guid.NewGuid()) timeDomain
        let allowDefinition, world =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:Allow")
                BuiltIn.textShape
                "known matching atom"
                initial
            |> Result.defaultWith failwith
        let denyDefinition, world =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:Deny")
                BuiltIn.textShape
                "known non-matching atom"
                world
            |> Result.defaultWith failwith
        let unknownDefinition, _ =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:Unsupported")
                BuiltIn.textShape
                "unsupported atom"
                world
            |> Result.defaultWith failwith

        let atom (definition: ConstraintDefinition) value =
            AtomicConstraint
                { Constraint = definition.Reference
                  Parameters = TextValue value }

        let evaluate (requirement: ConstraintRequirement) =
            if requirement.Constraint = allowDefinition.Reference then
                ConstraintAtomEvaluation.satisfied
            elif requirement.Constraint = denyDefinition.Reference then
                ConstraintAtomEvaluation.unsatisfied "known atom did not match"
            else
                ConstraintAtomEvaluation.unsupported unknownDefinition.Name

        let unknown = atom unknownDefinition "protected-secret"
        let allow = atom allowDefinition "match"
        let deny = atom denyDefinition "miss"
        let expressions =
            [ AllOf [ unknown; deny ]
              AllOf [ deny; unknown ]
              AnyOf [ unknown; allow ]
              AnyOf [ allow; unknown ]
              Not unknown
              AllOf [ allow; AnyOf [ deny; Not unknown ] ] ]

        let results = expressions |> List.map (ConstraintExpression.evaluate evaluate)

        Assert.That(results |> List.forall (fun result -> result.Outcome = Indeterminate), Is.True)
        Assert.That(
            results |> List.forall (fun result -> result.DiagnosticCategory = UnsupportedConstraint),
            Is.True)
        Assert.That(
            results
            |> List.forall (fun result -> result.UnsupportedConstraints = [ unknownDefinition.Name ]),
            Is.True)
        Assert.That(results |> List.forall (fun result -> not (result.Reason.Contains "protected-secret")), Is.True)

        let reordered =
            ConstraintExpression.evaluate evaluate (AnyOf [ allow; unknown ])

        Assert.That(reordered.Reason, Is.EqualTo((ConstraintExpression.evaluate evaluate (AnyOf [ unknown; allow ])).Reason))

    [<Test>]
    member _.``Cooling closes the loop in both directions`` () =
        let initial = Cooling.initial "primary" 20.0M 24.0M
        let operatorChange = Cooling.apply (SetTargetTemperature 26.0M) initial
        let sensorChange = Cooling.apply (RecordMeasurement 28.0M) operatorChange.After

        Assert.That(initial.CoolingEnabled, Is.True)
        Assert.That(operatorChange.After.CoolingEnabled, Is.False)
        Assert.That(sensorChange.After.CoolingEnabled, Is.True)
        Assert.That(sensorChange.After.Revision, Is.EqualTo 2L)

    [<Test>]
    member _.``Cooling state has a lossless ShapeValue representation`` () =
        let state = Cooling.initial "secondary" 18.5M 19.25M
        let decoded = Cooling.encodeState state |> Cooling.tryDecodeState

        match decoded with
        | Ok value ->
            Assert.That(value.Loop, Is.EqualTo state.Loop)
            Assert.That(value.TargetTemperature, Is.EqualTo state.TargetTemperature)
            Assert.That(value.CoolingEnabled, Is.EqualTo state.CoolingEnabled)
        | Error message -> Assert.Fail message

    [<Test>]
    member _.``event streams use optimistic append and deterministic folding`` () =
        let stream = EventStream.empty "cooling-primary"
        let updated = EventStream.append -1L [ 2; 3 ] stream |> Result.defaultWith failwith
        let stale = EventStream.append -1L [ 4 ] updated

        Assert.That(EventStream.version updated, Is.EqualTo 1L)
        Assert.That(EventStream.fold (+) 0 updated, Is.EqualTo 5)
        Assert.That(stale |> Result.isError, Is.True)

    [<Test>]
    member _.``flow fan-out waits and fan-in becomes ready once dependencies complete`` () =
        let operation: OperationReference =
            { Name = name "Brontide.Minimal.Tests:Flow.Operation" }

        let acquire = name "brontide-minimal.tests.acquire"
        let cool = name "brontide-minimal.tests.cool"
        let inspect = name "brontide-minimal.tests.inspect"
        let finish = name "brontide-minimal.tests.finish"

        let step stepName dependencies =
            { Name = stepName
              Operation = operation
              DependsOn = Set.ofList dependencies }

        let definition =
            Flow.tryCreate
                (name "brontide-minimal.tests.flow")
                [ step acquire []; step cool [ acquire ]; step inspect [ acquire ]; step finish [ cool; inspect ] ]
            |> Result.defaultWith failwith

        let execution = issuedExecution ()

        let state0 = Flow.start definition
        let state1 = Flow.markRunning acquire execution definition state0 |> Result.defaultWith failwith
        let state2 = Flow.complete acquire UnitValue definition state1 |> Result.defaultWith failwith
        let state3 = Flow.markRunning cool execution definition state2 |> Result.defaultWith failwith
        let state4 = Flow.complete cool UnitValue definition state3 |> Result.defaultWith failwith

        let readyAfterAcquire = Flow.readySteps state2 |> Set.ofList
        Assert.That(Set.contains cool readyAfterAcquire, Is.True)
        Assert.That(Set.contains inspect readyAfterAcquire, Is.True)
        Assert.That(Set.count readyAfterAcquire, Is.EqualTo 2)
        Assert.That(Flow.readySteps state4, Does.Not.Contain finish)

        let state5 = Flow.markRunning inspect execution definition state4 |> Result.defaultWith failwith
        let state6 = Flow.complete inspect UnitValue definition state5 |> Result.defaultWith failwith
        Assert.That(Flow.readySteps state6, Does.Contain finish)

    [<Test>]
    member _.``flow definitions reject cycles`` () =
        let operation: OperationReference =
            { Name = name "Brontide.Minimal.Tests:Operation" }

        let a = name "brontide-minimal.tests.a"
        let b = name "brontide-minimal.tests.b"

        let result =
            Flow.tryCreate
                (name "brontide-minimal.tests.cycle")
                [ { Name = a; Operation = operation; DependsOn = Set.singleton b }
                  { Name = b; Operation = operation; DependsOn = Set.singleton a } ]

        Assert.That(result |> Result.isError, Is.True)
