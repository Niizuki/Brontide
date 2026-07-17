namespace Linen.Composition.Tests

open NUnit.Framework
open Linen.Model
open Linen.Vocabularies.Imaging
open Linen.Experimental.Composition

module private Helpers =
    let name value = CanonicalName.create value

    let capability () = name "linen.imaging.execute"

    let cpu () =
        Composition.cpuImageComponent (name "linen.composition.cpu") (capability ()) 10

    let graph () =
        Composition.tryCreate [ cpu () ] [] |> Result.defaultWith failwith

    let image () = Image.tryCreate 2 1 Grayscale8 [| 0uy; 200uy |] |> Result.defaultWith failwith

open Helpers

[<TestFixture>]
type CompositionTests() =
    [<Test>]
    member _.``provider selection is explicit and explained`` () =
        let intent =
            { Capability = capability ()
              PreferredProviders = Set.singleton (name "linen.composition.cpu")
              OpposedProviders = Set.empty }

        let selected, explanation =
            Composition.select intent (graph ())
            |> Result.defaultWith (fun _ -> failwith "Provider selection failed.")

        Assert.That(CanonicalName.value selected.Name, Is.EqualTo "linen.composition.cpu")
        Assert.That(List.length explanation.Accepted, Is.EqualTo 2)

    [<Test>]
    member _.``opposition excludes a provider rather than silently weakening intent`` () =
        let intent =
            { Capability = capability ()
              PreferredProviders = Set.empty
              OpposedProviders = Set.singleton (name "linen.composition.cpu") }

        match Composition.select intent (graph ()) with
        | Error explanation ->
            Assert.That(explanation.Provider.IsNone, Is.True)
            Assert.That(List.length explanation.Rejected, Is.EqualTo 1)
        | Ok _ -> Assert.Fail "An opposed provider was selected."

    [<Test>]
    member _.``boxed values preserve shape and boundary claims`` () =
        let boundary = name "linen.tests.external-boundary"
        let claim = name "linen.tests.claim"
        let boxed = BoxedValue.create BuiltIn.textShape (TextValue "safe") boundary [ claim, "verified" ]

        Assert.That(BoxedValue.tryUnbox BuiltIn.integerShape boxed |> Result.isError, Is.True)

        match BoxedValue.tryUnbox BuiltIn.textShape boxed with
        | Ok(TextValue value) -> Assert.That(value, Is.EqualTo "safe")
        | _ -> Assert.Fail "The compatible boxed value did not round-trip."

        Assert.That(CanonicalName.value (BoxedValue.boundary boxed), Is.EqualTo "linen.tests.external-boundary")

    [<Test>]
    member _.``the CPU image provider executes the deterministic pipeline`` () =
        let intent =
            { Capability = capability ()
              PreferredProviders = Set.empty
              OpposedProviders = Set.empty }

        let selected, _ =
            Composition.select intent (graph ())
            |> Result.defaultWith (fun _ -> failwith "Provider selection failed.")
        let result = Composition.executeImage Invert (image ()) selected |> Result.defaultWith failwith

        let pixels = Image.pixels result
        Assert.That(pixels[0], Is.EqualTo 255uy)
        Assert.That(pixels[1], Is.EqualTo 55uy)

    [<Test>]
    member _.``mixed image workspaces preserve independent layers`` () =
        let source = image ()
        let sourceName = name "linen.workspace.source"
        let derivedName = name "linen.workspace.derived"

        let withSource =
            Workspace.addLayer { Name = sourceName; Image = source; Visible = true } Workspace.empty
            |> Result.defaultWith failwith

        let derived = Image.apply (Threshold 100uy) source

        let workspace =
            Workspace.addLayer { Name = derivedName; Image = derived; Visible = true } withSource
            |> Result.defaultWith failwith

        let hidden = Workspace.setVisibility sourceName false workspace |> Result.defaultWith failwith

        Assert.That(List.length hidden.Layers, Is.EqualTo 2)
        Assert.That(hidden.Layers[0].Visible, Is.False)
        let pixels = Image.pixels hidden.Layers[1].Image
        Assert.That(pixels[0], Is.EqualTo 0uy)
        Assert.That(pixels[1], Is.EqualTo 255uy)

    [<Test>]
    member _.``composition reports missing required capabilities`` () =
        let unavailable = name "linen.tests.unavailable"

        let candidate =
            { cpu () with
                Dependencies = [ { Capability = unavailable; Strength = RequiredGenericContract } ] }

        let composition = Composition.tryCreate [ candidate ] [] |> Result.defaultWith failwith
        Assert.That(Composition.validate composition |> List.length, Is.EqualTo 1)

    [<Test>]
    member _.``optimisation eligibility is declared and inspectable`` () =
        let required = Set.ofList [ Pure; Deterministic; NoIo ]
        let declared = Set.ofList [ Pure; Deterministic ]

        match Composition.optimisationEligibility required declared with
        | Ineligible missing -> Assert.That(Set.contains NoIo missing, Is.True)
        | Eligible -> Assert.Fail "An implementation missing NoIo was marked eligible."
