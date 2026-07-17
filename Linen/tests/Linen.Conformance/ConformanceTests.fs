namespace Linen.Conformance

open System
open NUnit.Framework
open Linen.Model
open Linen.Kernel

module private Helpers =
    let name value = CanonicalName.create value

    let get = function
        | Ok value -> value
        | Error message -> failwith message

    let operationReference: OperationReference =
        { Name = name "linen.tests.echo"
          Version = 1 }

    let prepareWorld () =
        let initial = World.create(Guid.Parse "63a31bb8-c202-45ae-a44e-276c24677e87")
        let actor, withActor = World.issueActor (name "linen.tests.actor") initial

        let operation: OperationDefinition =
            { Reference = operationReference
              Description = "Echo a text value."
              CommandShape = BuiltIn.textShape
              ResultShape = BuiltIn.textShape
              Constraints = [] }

        let withOperation = World.registerOperation operation withActor |> get

        let capability, withCapability =
            World.createCapability
                (name "linen.tests.echo-capability")
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
        match CanonicalName.tryCreate ".linen.bad" with
        | Error message -> Assert.That(message, Is.EqualTo "A canonical name cannot start or end with a dot.")
        | Ok _ -> Assert.Fail "The invalid name was accepted."

        Assert.That(CanonicalName.tryCreate "linen..bad" |> Result.isError, Is.True)
        Assert.That(CanonicalName.tryCreate "linen.good-name_1" |> Result.isOk, Is.True)

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
                              [ name "linen.provenance.echoed", "hello" ]
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
        let stranger, worldWithStranger = World.issueActor (name "linen.tests.stranger") world
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
            { Name = name "linen.tests.sample"
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
    member _.``delegated capabilities cannot broaden authority`` () =
        let _, world = prepareWorld ()

        let parentCapability, withParent =
            World.createCapability
                (name "linen.tests.parent")
                (Set.singleton operationReference)
                None
                world
            |> get

        let unknownOperation: OperationReference =
            { Name = name "linen.tests.unknown"
              Version = 1 }

        let broadened =
            World.createCapability
                (name "linen.tests.broadened")
                (Set.ofList [ operationReference; unknownOperation ])
                (Some parentCapability.Reference)
                withParent

        Assert.That(broadened |> Result.isError, Is.True)
