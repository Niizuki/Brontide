namespace Brontide.Minimal.Interchange.Tests.Portable

open System.Text.Json
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// This stack's fixtures declare the same contract the neutral layer declares.
///
/// PB1 declared only the Cooling fixture, so each stack authored its own Catalog fixture and the two
/// drifted: the Operation names and one `providerSpecific` flag disagreed. Negotiation matches both
/// exactly, so the drift made the two stacks unable to establish a Catalog binding at all — and it
/// stayed invisible while each stack ran Catalog only against itself. This suite compares the
/// negotiation surface against the checked-in declaration, so the next drift fails here rather than
/// in the cross-stack matrix, where the cause is much less obvious.
[<TestFixture>]
type PortableFixtureAlignmentTests() =

    [<Test>]
    member this.``the Cooling negotiation surface matches the neutral declaration``() =
        this.Check("fixture-contract.json", CoolingFixture.contract)

    [<Test>]
    member this.``the Catalog negotiation surface matches the neutral declaration``() =
        this.Check("catalog-fixture-contract.json", CatalogFixture.contract)

    member private _.Check(artifact: string, contract: ContractDocument) =
        use document = readNeutral [ "vectors"; artifact ]
        let root = document.RootElement

        let reference (element: JsonElement) =
            let name = element.GetProperty("name").GetString() |> str
            let version = element.GetProperty("version").GetInt32()
            $"{name}@{version}"

        let flag value = if value then "true" else "false"

        let declared name =
            root.GetProperty(name: string).EnumerateArray() |> List.ofSeq

        assertAll (fun () ->
            Assert.That(contract.ContractVersion, Is.EqualTo(root.GetProperty("contractVersion").GetInt32()))
            Assert.That(PortableComponentRef.text contract.Component, Is.EqualTo(reference (root.GetProperty "component")))
            Assert.That(PortableProviderRef.text contract.Provider, Is.EqualTo(reference (root.GetProperty "provider")))

            Assert.That(
                contract.Provisions
                |> List.map (fun provision ->
                    $"{DependencyKind.token provision.Kind} {PortableDependencyRef.text provision.Reference} providerSpecific={flag provision.ProviderSpecific}"),
                Is.EquivalentTo(
                    declared "provisions"
                    |> List.map (fun provision ->
                        let kind = provision.GetProperty("kind").GetString() |> str
                        let providerSpecific = flag (provision.GetProperty("providerSpecific").GetBoolean())
                        let dependency = reference (provision.GetProperty "reference")
                        $"{kind} {dependency} providerSpecific={providerSpecific}")
                ),
                "Provisions differ from the neutral declaration."
            )

            Assert.That(
                contract.Requirements
                |> List.map (fun requirement ->
                    let strength = RequirementStrength.token requirement.Strength
                    $"{DependencyKind.token requirement.Kind} {PortableDependencyRef.text requirement.Reference} {strength} providerSpecific={flag requirement.ProviderSpecific}"),
                Is.EquivalentTo(
                    declared "requirements"
                    |> List.map (fun requirement ->
                        let kind = requirement.GetProperty("kind").GetString() |> str
                        let strength = requirement.GetProperty("strength").GetString() |> str
                        let providerSpecific = flag (requirement.GetProperty("providerSpecific").GetBoolean())
                        let dependency = reference (requirement.GetProperty "reference")
                        $"{kind} {dependency} {strength} providerSpecific={providerSpecific}")
                ),
                "Requirements differ from the neutral declaration."
            )

            Assert.That(
                contract.Operations
                |> List.map (fun operation ->
                    let fragments =
                        operation.RequiredFragments |> List.map PortableFragmentRef.text |> String.concat ","

                    let flavors = operation.ResourceFlavors |> String.concat ","

                    $"{PortableOperationRef.text operation.Reference} in={PortableShapeRef.text operation.InputShape}"
                    + $" out={PortableShapeRef.text operation.ResultShape} detail={PortableShapeRef.text operation.DetailShape}"
                    + $" fragments=[{fragments}] flavors=[{flavors}]"),
                Is.EquivalentTo(
                    declared "operations"
                    |> List.map (fun operation ->
                        let fragments =
                            operation.GetProperty("requiredFragments").EnumerateArray()
                            |> Seq.map reference
                            |> String.concat ","

                        let flavors =
                            operation.GetProperty("resourceFlavors").EnumerateArray()
                            |> Seq.map (fun flavor -> flavor.GetString() |> str)
                            |> String.concat ","

                        let name = reference (operation.GetProperty "reference")
                        let input = reference (operation.GetProperty "inputShape")
                        let result = reference (operation.GetProperty "resultShape")
                        let detail = reference (operation.GetProperty "detailShape")

                        $"{name} in={input} out={result} detail={detail}"
                        + $" fragments=[{fragments}] flavors=[{flavors}]")
                ),
                "Operation declarations differ from the neutral declaration."
            )

            let representation = root.GetProperty "representation"

            Assert.That(
                contract.Representation.Representation,
                Is.EqualTo(representation.GetProperty("representation").GetString())
            )

            Assert.That(contract.Representation.Framing, Is.EqualTo(representation.GetProperty("framing").GetString()))

            Assert.That(
                contract.Representation.ResourceFlavors,
                Is.EquivalentTo(
                    representation.GetProperty("resourceFlavors").EnumerateArray()
                    |> Seq.map (fun flavor -> flavor.GetString() |> str)
                )
            )

            Assert.That(
                contract.Representation.AcceptedResourceHandles,
                Is.EquivalentTo(
                    representation.GetProperty("acceptedResourceHandles").EnumerateArray()
                    |> Seq.map (fun handle -> handle.GetString() |> str)
                )
            )

            // Every Shape the neutral declaration names is declared here. This stack may declare more
            // — the Cooling fixture adds the encoding-edge Shapes the golden encodings need — but it
            // may not omit one the contract's Operations depend on.
            let stackShapes =
                contract.Shapes |> List.map (fun shape -> PortableShapeRef.text shape.Reference) |> Set.ofList

            let neutralShapes =
                declared "shapes"
                |> List.map (fun shape -> reference (shape.GetProperty "reference"))
                |> Set.ofList

            Assert.That(
                Set.isSubset neutralShapes stackShapes,
                Is.True,
                $"Missing Shapes: %A{Set.difference neutralShapes stackShapes}"
            ))
