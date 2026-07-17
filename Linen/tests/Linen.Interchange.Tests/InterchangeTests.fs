namespace Linen.Interchange.Tests

open NUnit.Framework
open Linen.Model
open Linen.Binding
open Linen.Vocabularies.Cooling

module private Helpers =
    let wire name version = { Name = name; Version = version }

    let manifest implementation =
        { ProtocolVersion = 1
          Implementation = implementation
          Shapes = Set.ofList [ wire "linen.cooling.state" 1 ]
          Operations = Set.ofList [ wire "linen.cooling.apply" 1 ] }

open Helpers

[<TestFixture>]
type InterchangeTests() =
    [<Test>]
    member _.``external manifests negotiate data contracts without shared CLR types`` () =
        let local = manifest "linen-fsharp"
        let remote = manifest "external-runtime"

        let negotiated =
            Manifest.negotiate local.Shapes local.Operations local remote
            |> Result.defaultWith failwith

        Assert.That(negotiated.RemoteImplementation, Is.EqualTo "external-runtime")
        Assert.That(Set.count negotiated.Shapes, Is.EqualTo 1)
        Assert.That(Set.count negotiated.Operations, Is.EqualTo 1)

    [<Test>]
    member _.``protocol mismatches fail before value exchange`` () =
        let local = manifest "linen-fsharp"
        let remote = { manifest "external-runtime" with ProtocolVersion = 2 }

        Assert.That(Manifest.negotiate Set.empty Set.empty local remote |> Result.isError, Is.True)

    [<Test>]
    member _.``manifests have a JSON round trip`` () =
        let source = manifest "linen-fsharp"
        let decoded = Manifest.toJson source |> Manifest.tryFromJson |> Result.defaultWith failwith

        Assert.That(decoded.Implementation, Is.EqualTo source.Implementation)
        Assert.That(Set.count decoded.Shapes, Is.EqualTo(Set.count source.Shapes))

    [<Test>]
    member _.``ShapeValue codec round trips nested values and fragments`` () =
        let fragment: FragmentReference =
            { Name = CanonicalName.create "linen.tests.fragment"
              Version = 1 }

        let source =
            RecordValue(
                Map.ofList
                    [ "name", TextValue "sample"
                      "values", SequenceValue [ IntegerValue 1L; IntegerValue 2L ] ],
                Map.ofList [ fragment, BooleanValue true ]
            )

        let decoded = ValueCodec.encode source |> ValueCodec.decode |> Result.defaultWith failwith

        match decoded with
        | RecordValue(fields, fragments) ->
            match fields["name"] with
            | TextValue value -> Assert.That(value, Is.EqualTo "sample")
            | _ -> Assert.Fail "The text field changed type."

            Assert.That(Map.containsKey fragment fragments, Is.True)
        | _ -> Assert.Fail "The record value changed type."

    [<Test>]
    member _.``Cooling crosses the binding seam in both directions`` () =
        let state = Cooling.initial "primary" 20.0M 24.0M
        let outbound = state |> Cooling.encodeState |> ValueCodec.encode

        let inbound =
            outbound
            |> ValueCodec.decode
            |> Result.bind Cooling.tryDecodeState
            |> Result.defaultWith failwith

        let operatorChange = Cooling.apply (SetTargetTemperature 26.0M) inbound
        let sensorChange = Cooling.apply (RecordMeasurement 28.0M) operatorChange.After

        Assert.That(operatorChange.After.CoolingEnabled, Is.False)
        Assert.That(sensorChange.After.CoolingEnabled, Is.True)
