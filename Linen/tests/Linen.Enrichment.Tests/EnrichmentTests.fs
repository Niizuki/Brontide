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
