namespace Brontide.Minimal.Interchange.Tests.Portable

open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// Neutral vectors PB-64 through PB-69, and the three properties the Catalog group declares.
///
/// Every other vector group is authored against the Cooling fixture, which declares one Operation,
/// no detail Shape worth distinguishing, and no referenced handle. These cover only what Cooling
/// structurally cannot state: a negotiated Operation set, a repeated container whose elements are
/// themselves repeated containers, a declared detail Shape, and the provider-scoped addressing-only
/// handle.
///
/// The properties at the end are the Decision 10 practice: each quantifies over every scenario in
/// the group rather than over one case, because the PB6 defects were all invariants no single
/// expectation stated.
[<TestFixture>]
type PortableCatalogTests() =

    let handle = CatalogFixture.handle "catalog-provider" "primary"

    let negotiated () =
        expectOk (
            PortableNegotiation.negotiate
                CatalogFixture.contract
                CatalogFixture.contract
                Realization.FixedDirectCall
                "host"
                "provider"
                "test"
        )

    let catalogHost () =
        let handler = CatalogHandler()
        directHost CatalogFixture.contract handler, handler

    let invokeWith (host: PortableBindingHost) operation shape value resources =
        host.Invoke(operation, shape, value, permitted, resources).Result

    let itemIds value =
        match PortableRecord.tryField "items" value with
        | Some(PortableSequence items) ->
            items
            |> List.choose (fun item ->
                match PortableRecord.tryField "id" item with
                | Some(PortableText id) -> Some id
                | _ -> None)
        | _ -> []

    let tagsOf value id =
        match PortableRecord.tryField "items" value with
        | Some(PortableSequence items) ->
            items
            |> List.tryPick (fun item ->
                match PortableRecord.tryField "id" item, PortableRecord.tryField "tags" item with
                | Some(PortableText candidate), Some(PortableSequence tags) when candidate = id ->
                    tags
                    |> List.choose (fun tag ->
                        match tag with
                        | PortableText value -> Some value
                        | _ -> None)
                    |> Some
                | _ -> None)
        | _ -> None

    [<Test>]
    member _.``PB-64 a contract declaring two Operations negotiates both into one plan``() =
        let plan = negotiated ()
        let _, handler = catalogHost ()

        let upsert = expectOk (BindingPlan.operation CatalogFixture.upsert plan)
        let find = expectOk (BindingPlan.operation CatalogFixture.find plan)

        assertAll (fun () ->
            BindingPlan.operations plan
            |> shouldEqual [ CatalogFixture.upsert; CatalogFixture.find ]

            // Each Operation keeps its own three Shape positions. Cooling cannot show this: with one
            // Operation, "the plan's result Shape" and "this Operation's result Shape" coincide.
            upsert.InputShape |> shouldEqual CatalogFixture.upsertCommand
            upsert.ResultShape |> shouldEqual CatalogFixture.upsertResult
            find.InputShape |> shouldEqual CatalogFixture.findCommand
            find.ResultShape |> shouldEqual CatalogFixture.findResult
            Assert.That((upsert.ResultShape <> find.ResultShape), Is.True)

            // Both declare the same detail Shape, which is a fixture choice rather than a rule.
            upsert.DetailShape |> shouldEqual CatalogFixture.details
            find.DetailShape |> shouldEqual CatalogFixture.details

            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L, "Establishment activates no Operation."))

    [<Test>]
    member _.``PB-65 each request is routed by the Operation it names over one binding``() =
        let host, handler = catalogHost ()

        let upsert =
            invokeWith
                host
                CatalogFixture.upsert
                CatalogFixture.upsertCommand
                (CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "one" ] ])
                [ handle ]

        let find =
            invokeWith
                host
                CatalogFixture.find
                CatalogFixture.findCommand
                (CatalogFixture.findCommandValue [ "a" ])
                [ handle ]

        assertAll (fun () ->
            Assert.That(upsert.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)
            Assert.That(find.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)

            // Each result is shaped by its own Operation's result Shape: upsert answers with a
            // count, find with items. Routing is by the named Operation, not by the only one there
            // is. The count's meaning is a domain choice the contract does not fix, so only the
            // field's presence is asserted.
            let upsertValue = Option.get upsert.Value
            let findValue = Option.get find.Value
            Assert.That((PortableRecord.tryField "stored" upsertValue).IsSome, Is.True)
            Assert.That((PortableRecord.tryField "items" upsertValue).IsSome, Is.False)
            Assert.That((PortableRecord.tryField "items" findValue).IsSome, Is.True)
            Assert.That((PortableRecord.tryField "stored" findValue).IsSome, Is.False)

            // Sequential invocation is legal: single-invocation bounds concurrency at one request,
            // it does not cap how many a binding may serve over its lifetime.
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 2L))

    [<Test>]
    member _.``PB-66 a repeated container of repeated containers round trips exactly``() =
        let host, _ = catalogHost ()

        invokeWith
            host
            CatalogFixture.upsert
            CatalogFixture.upsertCommand
            (CatalogFixture.upsertCommandValue
                [ CatalogFixture.itemValue "a" "Alpha" [ "one"; "two" ]
                  CatalogFixture.itemValue "b" "Beta" [ "two" ]
                  CatalogFixture.itemValue "c" "Gamma" [] ])
            [ handle ]
        |> ignore

        let find =
            invokeWith
                host
                CatalogFixture.find
                CatalogFixture.findCommand
                (CatalogFixture.findCommandValue [ "c"; "a"; "b" ])
                [ handle ]

        let value = Option.get find.Value

        assertAll (fun () ->
            itemIds value |> shouldEqual [ "c"; "a"; "b" ]

            // The inner sequences survive independently of the outer one, and an empty one stays
            // empty rather than becoming absent or null.
            tagsOf value "c" |> shouldEqual (Some [])
            tagsOf value "a" |> shouldEqual (Some [ "one"; "two" ])
            tagsOf value "b" |> shouldEqual (Some [ "two" ]))

    [<Test>]
    member _.``PB-67 a semantic failure is shaped by the Operation's declared detail Shape``() =
        let host, handler = catalogHost ()
        let plan = negotiated ()

        // Nothing was stored, so the lookup cannot be satisfied.
        let find =
            invokeWith
                host
                CatalogFixture.find
                CatalogFixture.findCommand
                (CatalogFixture.findCommandValue [ "absent" ])
                [ handle ]

        let detailShape = (expectOk (BindingPlan.operation CatalogFixture.find plan)).DetailShape

        let declaredDetailFields =
            CatalogFixture.contract.Shapes
            |> List.pick (fun shape ->
                match shape.Body with
                | RecordBody(_, fields) when shape.Reference = detailShape ->
                    fields |> List.map (fun field -> field.Name) |> List.sort |> Some
                | _ -> None)

        assertAll (fun () ->
            // A semantic failure is an Outcome, not a protocol error and not an exception.
            Assert.That(find.FrameDecision, Is.EqualTo FrameDecision.Accept)
            Assert.That(find.ResultClass, Is.EqualTo ResultClass.OutcomeFailed)

            // The detail conforms to the Shape the Operation declares. The retained Catalog
            // experiment declared no detail Shape at all; the neutral contract requires all three
            // positions, and this is what that addition buys.
            let detail = Option.get find.Value

            let observedFields =
                match detail with
                | PortableRecord(fields, _) -> fields |> Map.toList |> List.map fst |> List.sort
                | _ -> []

            observedFields |> shouldEqual declaredDetailFields

            // The Shape is normative; the code's spelling is not. PB-48 already fixes that two
            // realizations may choose different local codes for the same portable category.
            match PortableRecord.tryField "code" detail with
            | Some(PortableText code) -> Assert.That(code, Is.Not.Empty)
            | _ -> Assert.Fail "The detail carries a text 'code' field."

            Assert.That(find.Observation.TerminalStatus, Is.EqualTo TerminalStatus.Failed)

            Assert.That(
                handler.ProviderEffectCount,
                Is.EqualTo 0L,
                "A lookup that stored nothing performed no effect."
            ))

    [<Test>]
    member _.``PB-68 an accepted handle addresses the domain without deciding its answer``() =
        let host, _ = catalogHost ()

        let stored =
            invokeWith
                host
                CatalogFixture.upsert
                CatalogFixture.upsertCommand
                (CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "one" ] ])
                [ handle ]

        let satisfiable =
            invokeWith
                host
                CatalogFixture.find
                CatalogFixture.findCommand
                (CatalogFixture.findCommandValue [ "a" ])
                [ handle ]

        // Every requested identifier is absent. A partial match is deliberately not used: the
        // fixture contract declares no partial-match rule, so a vector turning on one would assert
        // undeclared domain behaviour rather than the handle rule this vector is about.
        let unsatisfiable =
            invokeWith
                host
                CatalogFixture.find
                CatalogFixture.findCommand
                (CatalogFixture.findCommandValue [ "absent" ])
                [ handle ]

        assertAll (fun () ->
            // The same in-scope handle is admitted every time, and carries no octets.
            for result in [ stored; satisfiable; unsatisfiable ] do
                Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Accept)
                Assert.That(List.length result.Observation.ReferencedResources, Is.EqualTo 1)
                Assert.That((List.head result.Observation.ReferencedResources).Ownership, Is.EqualTo "provider-retained")
                Assert.That(result.Observation.CopyCount, Is.EqualTo 0L)

            // The outcomes still differ, so admitting the handle and admitting the request are two
            // decisions. Possession conveys where to look, never what may be done.
            Assert.That(satisfiable.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)
            Assert.That(unsatisfiable.ResultClass, Is.EqualTo ResultClass.OutcomeFailed))

    [<Test>]
    member _.``PB-69 a flavor outside the frozen plan is refused after establishment succeeded``() =
        let host, handler = catalogHost ()

        // copied-immutable-blob is a declared 0.1 flavor and the Cooling binding negotiates it. This
        // binding did not, so negotiating it elsewhere confers nothing here.
        let result =
            invokeWith
                host
                CatalogFixture.upsert
                CatalogFixture.upsertCommand
                (CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "one" ] ])
                [ blob "cooling-profile" ]

        assertAll (fun () ->
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.ProtocolError)

            // This is where a resource refusal splits between two categories. A flavor is a term of
            // the frozen contract, so refusing one is unsupported-contract whether it is reached
            // during negotiation (PB-29) or afterwards (here). Refusing a particular resource of a
            // negotiated flavor is invalid-payload instead: PB-28's out-of-scope handle and PB-26's
            // failed content hash.
            Assert.That(result.Category, Is.EqualTo(Some ProtocolCategory.UnsupportedContract))
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L)

            // The refused resource is still observed, and is not reported as an admission that
            // never completed.
            Assert.That(List.length result.Observation.ReferencedResources, Is.EqualTo 1)
            let observed = List.head result.Observation.ReferencedResources
            Assert.That(observed.Accepted, Is.False)
            Assert.That(observed.IntegrityVerified, Is.False))

    [<Test>]
    member _.``PB-70 a lookup answers for every identifier or for none``() =
        let host, handler = catalogHost ()

        invokeWith
            host
            CatalogFixture.upsert
            CatalogFixture.upsertCommand
            (CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "one" ] ])
            [ handle ]
        |> ignore

        let effectsAfterUpsert = handler.ProviderEffectCount

        // One identifier is held and one is not. The result Shape is a sequence of items with no
        // companion field for the ones that missed, so a partial answer would drop which identifier
        // was absent with no way for the caller to recover it.
        let partial =
            invokeWith
                host
                CatalogFixture.find
                CatalogFixture.findCommand
                (CatalogFixture.findCommandValue [ "a"; "absent" ])
                [ handle ]

        assertAll (fun () ->
            Assert.That(partial.FrameDecision, Is.EqualTo FrameDecision.Accept)
            Assert.That(partial.ResultClass, Is.EqualTo ResultClass.OutcomeFailed)

            // The detail Shape is where an absent identifier is reported.
            let detail = Option.get partial.Value
            Assert.That((PortableRecord.tryField "code" detail).IsSome, Is.True)
            Assert.That((PortableRecord.tryField "items" detail).IsSome, Is.False, "No partial result crosses.")

            Assert.That(
                handler.ProviderEffectCount,
                Is.EqualTo effectsAfterUpsert,
                "A lookup that answered for nothing performed no further effect."
            ))

    [<Test>]
    member _.``PB-71 the upsert count answers this request and not the session total``() =
        let host, _ = catalogHost ()

        let storedCount (result: InteractionResult) =
            match result.Value |> Option.bind (PortableRecord.tryField "stored") with
            | Some(PortableInteger value) -> value
            | _ -> -1L

        let first =
            invokeWith
                host
                CatalogFixture.upsert
                CatalogFixture.upsertCommand
                (CatalogFixture.upsertCommandValue
                    [ CatalogFixture.itemValue "a" "Alpha" [ "one" ]
                      CatalogFixture.itemValue "b" "Beta" [ "two" ] ])
                [ handle ]

        // Different, previously unseen items. A session running total would answer 3 here.
        let second =
            invokeWith
                host
                CatalogFixture.upsert
                CatalogFixture.upsertCommand
                (CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "c" "Gamma" [] ])
                [ handle ]

        assertAll (fun () ->
            Assert.That(storedCount first, Is.EqualTo 2L)

            Assert.That(
                storedCount second,
                Is.EqualTo 1L,
                "The count answers how many items this request stored, not how many the session holds."
            ))

    // ----------------------------------------------------------------------------------------
    // Properties over the whole group (Decision 10).
    //
    // A per-vector expectation states what one case should produce. These state what must hold of
    // every case, including ones nobody wrote, which is the class of claim PB6 found missing: all
    // three of its defects were invariants that every individual expectation happened to satisfy.
    // ----------------------------------------------------------------------------------------

    /// Every interaction the group performs, paired with the Operation the request named, so a
    /// property can quantify over them. The observation does not carry the invoked Operation, so it
    /// is recorded here rather than inferred.
    member private _.EveryGroupInteraction() =
        let host, _ = catalogHost ()

        [ CatalogFixture.upsert,
          invokeWith
              host
              CatalogFixture.upsert
              CatalogFixture.upsertCommand
              (CatalogFixture.upsertCommandValue
                  [ CatalogFixture.itemValue "a" "Alpha" [ "one"; "two" ]
                    CatalogFixture.itemValue "c" "Gamma" [] ])
              [ handle ]

          CatalogFixture.find,
          invokeWith
              host
              CatalogFixture.find
              CatalogFixture.findCommand
              (CatalogFixture.findCommandValue [ "c"; "a" ])
              [ handle ]

          CatalogFixture.find,
          invokeWith
              host
              CatalogFixture.find
              CatalogFixture.findCommand
              (CatalogFixture.findCommandValue [ "absent" ])
              [ handle ]

          CatalogFixture.upsert,
          invokeWith
              host
              CatalogFixture.upsert
              CatalogFixture.upsertCommand
              (CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "one" ] ])
              [ blob "cooling-profile" ] ]

    [<Test>]
    member this.``CATALOG-P1 every named Operation is a member of the established plan``() =
        let planOperations = BindingPlan.operations (negotiated ())
        let interactions = this.EveryGroupInteraction()

        assertAll (fun () ->
            Assert.That(List.isEmpty interactions, Is.False)

            for operation, result in interactions do
                result.Observation.NegotiatedOperations |> shouldEqual planOperations

                Assert.That(
                    List.contains operation planOperations,
                    Is.True,
                    "A request's Operation is always a member of its own plan."
                ))

    [<Test>]
    member this.``CATALOG-P2 no octets ever cross for this binding's only negotiated flavor``() =
        let interactions = this.EveryGroupInteraction()

        assertAll (fun () ->
            for _, result in interactions do
                Assert.That(
                    result.Observation.CopyCount,
                    Is.EqualTo 0L,
                    "The addressing-only handle is the only negotiated flavor and it carries no octets."
                )

                // Quantifying over *accepted* resources rather than over reported ones is the point.
                // A refused resource is still observed, so the reported set legitimately contains
                // flavors the plan never froze; what must never happen is one of them being
                // reported as admitted.
                for resource in result.Observation.ReferencedResources do
                    if resource.Accepted then
                        Assert.That(
                            resource.Flavor,
                            Is.EqualTo ResourceFlavor.AddressingOnlyHandleToken,
                            "An accepted resource is always of a flavor the plan froze."
                        )
                    else
                        Assert.That(
                            resource.IntegrityVerified,
                            Is.False,
                            "An admission that never completed claims no integrity check."
                        ))

    [<Test>]
    member this.``CATALOG-P3 an outcome never carries both a result and a detail``() =
        let interactions = this.EveryGroupInteraction()

        assertAll (fun () ->
            for _, result in interactions do
                match result.ResultClass with
                | ResultClass.OutcomeSucceeded ->
                    Assert.That(result.Value.IsSome, Is.True, "A success carries its result.")
                    Assert.That(result.Observation.TerminalStatus, Is.EqualTo TerminalStatus.Succeeded)
                | ResultClass.OutcomeFailed ->
                    Assert.That(result.Value.IsSome, Is.True, "A shaped failure carries its detail.")

                    Assert.That(
                        result.Observation.TerminalStatus,
                        Is.EqualTo TerminalStatus.Failed,
                        "A failed Outcome never reports a succeeded terminal status."
                    )
                | _ ->
                    Assert.That(
                        result.Value.IsNone,
                        Is.True,
                        "A non-Outcome result class carries neither a result nor a detail."
                    )

                    Assert.That(
                        (result.Observation.TerminalStatus <> TerminalStatus.Succeeded),
                        Is.True,
                        "Success is never fabricated for a refused request."
                    ))
