namespace Linen.Enrichment.Tests

open NUnit.Framework
open Linen.Model
open Linen.Experimental.Enrichment

module private Helpers =
    let name value = CanonicalName.create value

    let fragment () : FragmentReference =
        { Name = name "linen.tests.fragment"
          Version = 1 }

    let vocabulary () = name "linen.tests.vocabulary"

    let provider () : EnrichmentProvider =
        { Name = name "linen.tests.provider"
          Vocabularies = Set.singleton (vocabulary ())
          Resolve =
            fun request ->
                Ok
                    [ { Provider = name "linen.tests.provider"
                        Fragment = fragment ()
                        Value = TextValue request.Subject
                        Claims = [ name "linen.tests.enriched", request.Subject ] } ] }

open Helpers

[<TestFixture>]
type EnrichmentTests() =
    [<Test>]
    member _.``providers are explicit and selected by name`` () =
        let provider = provider ()
        let registry = ProviderRegistry.register provider ProviderRegistry.empty |> Result.defaultWith failwith

        let request =
            { Subject = "subject-1"
              Vocabulary = vocabulary ()
              Input = UnitValue }

        let enrichment = ProviderRegistry.enrich provider.Name request registry |> Result.defaultWith failwith
        Assert.That(enrichment.Head.Provider |> CanonicalName.value, Is.EqualTo "linen.tests.provider")

    [<Test>]
    member _.``unsupported vocabularies are not silently inferred`` () =
        let provider = provider ()
        let registry = ProviderRegistry.register provider ProviderRegistry.empty |> Result.defaultWith failwith

        let request =
            { Subject = "subject-1"
              Vocabulary = name "linen.tests.unsupported"
              Input = UnitValue }

        Assert.That(ProviderRegistry.enrich provider.Name request registry |> Result.isError, Is.True)

    [<Test>]
    member _.``enrichment attaches fragments without mutating base fields`` () =
        let provider = provider ()
        let fragment = fragment ()
        let baseValue = RecordValue(Map.ofList [ "name", TextValue "base" ], Map.empty)

        let item =
            { Provider = provider.Name
              Fragment = fragment
              Value = TextValue "derived"
              Claims = [] }

        let attached = Enrichment.attach [ item ] baseValue |> Result.defaultWith failwith

        match attached with
        | RecordValue(fields, fragments) ->
            Assert.That(fields["name"], Is.EqualTo(TextValue "base"))
            Assert.That(fragments.ContainsKey fragment, Is.True)
        | _ -> Assert.Fail "Enrichment changed the base value kind."

    [<Test>]
    member _.``pointer temperature Enrichment is targeted local additive and route free`` () =
        let pointerMove: OperationReference =
            { Name = name "input.pointer.move"
              Version = 1 }

        let thermalContext: FragmentReference =
            { Name = name "experiment.thermal-context"
              Version = 1 }

        let declaration: TargetedEnrichmentDeclaration =
            { Name = name "linen.enrichment.thermal-from-telemetry"
              Target = pointerMove
              Fragment = thermalContext
              RequiredSources = Set.singleton "telemetry"
              Derive =
                fun values ->
                    match values["telemetry"] with
                    | RecordValue(fields, _) ->
                        match Map.tryFind "temperature" fields with
                        | Some temperature ->
                            Ok(RecordValue(Map.ofList [ "temperature", temperature ], Map.empty))
                        | None -> Error "Telemetry has no temperature."
                    | _ -> Error "Telemetry must be a record." }

        let pointerInput =
            RecordValue(
                Map.ofList [ "x", IntegerValue 10L; "y", IntegerValue 20L ],
                Map.empty
            )

        let telemetry =
            { Key = "telemetry"
              Value = RecordValue(Map.ofList [ "temperature", IntegerValue 31L ], Map.empty)
              Provenance = "result of explicit DeviceTelemetry.Read Execution" }

        let resolved =
            TargetedEnrichment.resolve pointerMove thermalContext declaration [ telemetry ] pointerInput
            |> Result.defaultWith failwith

        match resolved.Input with
        | RecordValue(fields, fragments) ->
            Assert.That(fields["x"], Is.EqualTo(IntegerValue 10L))
            Assert.That(fragments.ContainsKey thermalContext, Is.True)
            Assert.That(resolved.Sources["telemetry"], Does.StartWith "result of explicit")
        | _ -> Assert.Fail "Enrichment changed the pointer value kind."

        Assert.That(
            TargetedEnrichment.resolve pointerMove thermalContext declaration [] pointerInput
            |> Result.isError,
            Is.True
        )

        Assert.That(
            typeof<TargetedEnrichmentDeclaration>.GetProperties()
            |> Seq.exists (fun property -> property.Name.Contains "Route"),
            Is.False
        )
