namespace Brontide.Minimal.Conformance

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

module private Helpers =
    let name value = CanonicalName.create value

    let get = function
        | Ok value -> value
        | Error message -> failwith message

    let operationReference: OperationReference =
        { Name = name "brontide-minimal.tests.echo"
          Version = 1 }

    let prepareWorld () =
        let initial = World.create(Guid.Parse "63a31bb8-c202-45ae-a44e-276c24677e87")
        let actor, withActor = World.issueActor (name "brontide-minimal.tests.actor") initial

        let operation: OperationDefinition =
            { Reference = operationReference
              Description = "Echo a text value."
              CommandShape = BuiltIn.textShape
              ResultShape = BuiltIn.textShape
              Constraints = [] }

        let withOperation = World.registerOperation operation withActor |> get

        let capability, withCapability =
            World.createCapability
                (name "brontide-minimal.tests.echo-capability")
                (Set.singleton operationReference)
                None
                withOperation
            |> get

        let ready = World.grant actor.Reference capability.Reference withCapability |> get
        actor, ready

open Helpers

[<TestFixture>]
type BaseConformance() =
    [<Test>]
    member _.``canonical names reject ambiguous authored syntax`` () =
        match CanonicalName.tryCreate ".brontide-minimal.bad" with
        | Error message -> Assert.That(message, Is.EqualTo "A canonical name cannot start or end with a dot.")
        | Ok _ -> Assert.Fail "The invalid name was accepted."

        Assert.That(CanonicalName.tryCreate "brontide-minimal..bad" |> Result.isError, Is.True)
        Assert.That(CanonicalName.tryCreate "brontide-minimal.good-name_1" |> Result.isOk, Is.True)

    [<Test>]
    member _.``the same world and request produce the same transition`` () =
        let actor, world = prepareWorld ()

        let request =
            { Actor = actor.Reference
              Operation = operationReference
              Command = TextValue "hello"
              Occurrence = None
              Context = Map.empty }

        let environment =
            { LogicalTime = 42L
              ConstraintEvaluators = Map.empty
              Handlers =
                Map.ofList
                    [ operationReference,
                      fun execution ->
                          Ok(
                              execution.Command,
                              [],
                              [ name "brontide-minimal.provenance.echoed", "hello" ]
                          ) ] }

        let first = World.step environment world request
        let second = World.step environment world request

        Assert.That(first.Outcome.Execution.Value, Is.EqualTo second.Outcome.Execution.Value)
        Assert.That(first.Outcome.Status, Is.EqualTo second.Outcome.Status)

        match first.Outcome.Result, second.Outcome.Result with
        | Some(TextValue firstValue), Some(TextValue secondValue) ->
            Assert.That(firstValue, Is.EqualTo secondValue)
        | _ -> Assert.Fail "The deterministic echo result was not text."

        Assert.That(first.Provenance.Head.Object, Is.EqualTo second.Provenance.Head.Object)
        Assert.That(first.Outcome.Status, Is.EqualTo Succeeded)

    [<Test>]
    member _.``denial happens before a handler can produce effects`` () =
        let actor, world = prepareWorld ()
        let stranger, worldWithStranger = World.issueActor (name "brontide-minimal.tests.stranger") world
        let mutable invoked = false

        let request =
            { Actor = stranger.Reference
              Operation = operationReference
              Command = TextValue "forbidden"
              Occurrence = None
              Context = Map.empty }

        let environment =
            { LogicalTime = 0L
              ConstraintEvaluators = Map.empty
              Handlers =
                Map.ofList
                    [ operationReference,
                      fun execution ->
                          invoked <- true
                          Ok(execution.Command, [], []) ] }

        let result = World.step environment worldWithStranger request

        Assert.That(result.Outcome.Status, Is.EqualTo Denied)
        Assert.That(invoked, Is.False)
        Assert.That(result.EmittedEvents, Is.Empty)
        Assert.That(actor.Reference, Is.Not.EqualTo stranger.Reference)

    [<Test>]
    member _.``shape versions are additive and projections are explicit`` () =
        let world = World.create(Guid.Parse "63a31bb8-c202-45ae-a44e-276c24677e87")

        let v1: ShapeReference =
            { Name = name "brontide-minimal.tests.sample"
              Version = 1 }

        let v2 = { v1 with Version = 2 }

        let definition (reference: ShapeReference) (fields: RecordField list): ShapeDefinition =
            { Reference = reference
              Description = "Projection test"
              Body = RecordShape fields
              AcceptedFragments = Set.empty
              IsOpenToFragments = false }

        let field fieldName required =
            { Name = fieldName
              Shape = BuiltIn.textShape
              Required = required }

        let withV1 = World.registerShape (definition v1 [ field "name" true ]) world |> get

        let withV2 =
            World.registerShape (definition v2 [ field "name" true; field "note" false ]) withV1
            |> get

        let value =
            RecordValue(Map.ofList [ "name", TextValue "sample"; "note", TextValue "new" ], Map.empty)

        let projected = World.projectRecord v1 value withV2 |> get

        Assert.That(projected, Is.EqualTo(RecordValue(Map.ofList [ "name", TextValue "sample" ], Map.empty)))
        Assert.That(World.registerShape (definition v1 []) withV2 |> Result.isError, Is.True)

    [<Test>]
    member _.``open Velocity accepts and canonically projects an authored DirectionalVelocity Fragment`` () =
        let world = World.create(Guid.Parse "718b889d-7723-4cb7-a7a3-b8d777242530")

        let velocity: ShapeReference =
            { Name = name "Velocity"
              Version = 1 }

        let direction: ShapeReference =
            { Name = name "Bob.Direction"
              Version = 1 }

        let fragmentShape: ShapeReference =
            { Name = name "Bob.DirectionalVelocity.Fields"
              Version = 1 }

        let directional: FragmentReference =
            { Name = name "Bob.DirectionalVelocity"
              Version = 1 }

        let velocityDefinition: ShapeDefinition =
            { Reference = velocity
              Description = "Open velocity Shape"
              Body =
                RecordShape
                    [ { Name = "speed"
                        Shape = BuiltIn.integerShape
                        Required = true } ]
              AcceptedFragments = Set.empty
              IsOpenToFragments = true }

        let directionDefinition: ShapeDefinition =
            { Reference = direction
              Description = "Direction"
              Body = ScalarShape Text
              AcceptedFragments = Set.empty
              IsOpenToFragments = false }

        let fragmentShapeDefinition: ShapeDefinition =
            { Reference = fragmentShape
              Description = "Directional fields"
              Body =
                RecordShape
                    [ { Name = "direction"
                        Shape = direction
                        Required = true } ]
              AcceptedFragments = Set.empty
              IsOpenToFragments = false }

        let withVelocity = World.registerShape velocityDefinition world |> get
        let withDirection = World.registerShape directionDefinition withVelocity |> get
        let withFragmentShape = World.registerShape fragmentShapeDefinition withDirection |> get

        let ready =
            World.registerFragment
                { Reference = directional
                  Description = "Bob's authored velocity direction"
                  Shape = fragmentShape }
                withFragmentShape
            |> get

        let composed =
            RecordValue(
                Map.ofList [ "speed", IntegerValue 12L ],
                Map.ofList
                    [ directional,
                      RecordValue(Map.ofList [ "direction", TextValue "north" ], Map.empty) ]
            )

        let canonical = World.projectRecord velocity composed ready |> get
        let required =
            World.projectRecordWithFragments velocity (Set.singleton directional) composed ready
            |> get

        let baseOnly = RecordValue(Map.ofList [ "speed", IntegerValue 12L ], Map.empty)

        Assert.That(World.validateValue velocity composed ready |> Result.isOk, Is.True)
        Assert.That(canonical, Is.EqualTo(RecordValue(Map.ofList [ "speed", IntegerValue 12L ], Map.empty)))
        Assert.That(required, Is.EqualTo composed)
        Assert.That(
            World.validateContract velocity (Set.singleton directional) baseOnly ready
            |> Result.isError,
            Is.True
        )

    [<Test>]
    member _.``delegated capabilities cannot broaden authority`` () =
        let _, world = prepareWorld ()

        let parentCapability, withParent =
            World.createCapability
                (name "brontide-minimal.tests.parent")
                (Set.singleton operationReference)
                None
                world
            |> get

        let unknownOperation: OperationReference =
            { Name = name "brontide-minimal.tests.unknown"
              Version = 1 }

        let broadened =
            World.createCapability
                (name "brontide-minimal.tests.broadened")
                (Set.ofList [ operationReference; unknownOperation ])
                (Some parentCapability.Reference)
                withParent

        Assert.That(broadened |> Result.isError, Is.True)
