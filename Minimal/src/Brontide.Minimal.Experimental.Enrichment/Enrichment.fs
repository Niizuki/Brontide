namespace Brontide.Minimal.Experimental.Enrichment

open Brontide.Minimal.Model

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

type AvailableValue =
    { Key: string
      Value: ShapeValue
      Provenance: string }

type TargetedEnrichmentDeclaration =
    { Name: CanonicalName
      Target: OperationReference
      Fragment: FragmentReference
      RequiredSources: Set<string>
      Derive: Map<string, ShapeValue> -> Result<ShapeValue, string> }

type TargetedEnrichmentResolution =
    { Input: ShapeValue
      Provider: CanonicalName
      Target: OperationReference
      Fragment: FragmentReference
      Sources: Map<string, string> }

[<RequireQualifiedAccess>]
module ProviderRegistry =
    let empty = ProviderRegistry Map.empty

    let register (provider: EnrichmentProvider) (ProviderRegistry providers) =
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
    let attach (enrichments: Enrichment list) value =
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

[<RequireQualifiedAccess>]
module TargetedEnrichment =
    let resolve
        (target: OperationReference)
        (requiredFragment: FragmentReference)
        (declaration: TargetedEnrichmentDeclaration)
        (availableValues: AvailableValue list)
        (input: ShapeValue)
        =
        if declaration.Target <> target || declaration.Fragment <> requiredFragment then
            Error "The Enrichment declaration does not target that Operation and Fragment."
        else
            match input with
            | RecordValue(_, fragments) when Map.containsKey requiredFragment fragments ->
                Error "Enrichment can only add an absent Fragment."
            | RecordValue _ ->
                let duplicate =
                    availableValues
                    |> List.countBy _.Key
                    |> List.tryFind (fun (_, count) -> count > 1)

                match duplicate with
                | Some(key, _) -> Error $"The available source '{key}' is ambiguous."
                | None ->
                    let available = availableValues |> List.map (fun value -> value.Key, value) |> Map.ofList

                    let missing =
                        declaration.RequiredSources
                        |> Seq.tryFind (fun key -> not (Map.containsKey key available))

                    match missing with
                    | Some key -> Error $"The required source '{key}' is not available."
                    | None ->
                        let sourceValues =
                            declaration.RequiredSources
                            |> Seq.map (fun key -> key, available[key].Value)
                            |> Map.ofSeq

                        declaration.Derive sourceValues
                        |> Result.bind (fun fragmentValue ->
                            let item: Enrichment =
                                { Provider = declaration.Name
                                  Fragment = declaration.Fragment
                                  Value = fragmentValue
                                  Claims = [] }

                            Enrichment.attach [ item ] input
                            |> Result.map (fun enriched ->
                                { Input = enriched
                                  Provider = declaration.Name
                                  Target = target
                                  Fragment = requiredFragment
                                  Sources =
                                    declaration.RequiredSources
                                    |> Seq.map (fun key -> key, available[key].Provenance)
                                    |> Map.ofSeq }))
            | _ -> Error "Targeted Enrichment requires a record input Shape."
