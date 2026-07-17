namespace Linen.Experimental.Enrichment

open Linen.Model

type EnrichmentRequest =
    { Subject: string
      Vocabulary: CanonicalName
      Input: ShapeValue }

type Enrichment =
    { Provider: CanonicalName
      Fragment: FragmentReference
      Value: ShapeValue
      Claims: (CanonicalName * string) list }

type EnrichmentProvider =
    { Name: CanonicalName
      Vocabularies: Set<CanonicalName>
      Resolve: EnrichmentRequest -> Result<Enrichment list, string> }

type ProviderRegistry = private ProviderRegistry of Map<CanonicalName, EnrichmentProvider>

[<RequireQualifiedAccess>]
module ProviderRegistry =
    let empty = ProviderRegistry Map.empty

    let register provider (ProviderRegistry providers) =
        if Map.containsKey provider.Name providers then
            Error "That enrichment provider is already registered."
        else
            Ok(ProviderRegistry(Map.add provider.Name provider providers))

    let providersFor vocabulary (ProviderRegistry providers) =
        providers
        |> Map.toSeq
        |> Seq.map snd
        |> Seq.filter (fun provider -> Set.contains vocabulary provider.Vocabularies)
        |> Seq.toList

    let enrich providerName request (ProviderRegistry providers) =
        match Map.tryFind providerName providers with
        | None -> Error "The selected enrichment provider is not registered."
        | Some provider when not (Set.contains request.Vocabulary provider.Vocabularies) ->
            Error "The selected provider does not support that vocabulary."
        | Some provider -> provider.Resolve request

[<RequireQualifiedAccess>]
module Enrichment =
    let attach enrichments value =
        match value with
        | RecordValue(fields, existingFragments) ->
            let duplicates =
                enrichments
                |> List.map _.Fragment
                |> List.countBy id
                |> List.tryFind (fun (_, count) -> count > 1)

            match duplicates with
            | Some _ -> Error "Enrichment produced the same fragment more than once."
            | None ->
                let fragments =
                    enrichments
                    |> List.fold (fun state item -> Map.add item.Fragment item.Value state) existingFragments

                Ok(RecordValue(fields, fragments))
        | _ -> Error "Enrichment fragments can only be attached to record values."
