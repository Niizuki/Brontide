namespace Brontide.Minimal.Interchange.Tests.Portable

open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// Small edits over decoded control items, so an adversarial document can be built exactly rather
/// than hand-typed as bytes.
[<AutoOpen>]
module CborEdits =

    let withEntry key value item =
        match item with
        | CborMap entries -> CborMap((entries |> List.filter (fun (name, _) -> name <> key)) @ [ key, value ])
        | other -> other

    let withoutEntry key item =
        match item with
        | CborMap entries -> CborMap(entries |> List.filter (fun (name, _) -> name <> key))
        | other -> other

    let entry key item =
        match item with
        | CborMap entries -> entries |> List.pick (fun (name, value) -> if name = key then Some value else None)
        | _ -> failwith $"'{key}' is not a member of a non-map item."

    let replaceAt index value item =
        match item with
        | CborArray items -> CborArray(items |> List.mapi (fun position current -> if position = index then value else current))
        | other -> other

/// C1 and C5: negotiation, the Shape floor, projection, and forbidden content.
[<TestFixture>]
type PortableEstablishmentTests() =

    let catalog = catalogOf CoolingFixture.contract
    let limits = PortableLimits.declared

    let negotiate required offered =
        PortableNegotiation.negotiate required offered Realization.FixedDirectCall "host" "provider" "fixed"

    let establishedEndpoint () =
        let endpoint =
            PortableProviderEndpoint(CoolingFixture.contract, CoolingHandler(), Realization.FixedDirectCall)

        expectOk (endpoint.Establish(CoolingFixture.contract, "host")) |> ignore
        endpoint

    [<Test>]
    member _.``PB-01 an exactly equal contract establishes and freezes the plan``() =
        let handler = CoolingHandler()
        let plan = expectOk (negotiate CoolingFixture.contract CoolingFixture.contract)

        assertAll (fun () ->
            Assert.That(BindingPlan.contractVersion plan, Is.EqualTo 1)
            shouldEqual [ CoolingFixture.setEnabled ] (BindingPlan.operations plan)
            Assert.That(List.length (BindingPlan.satisfiedRequirements plan), Is.EqualTo 2)
            // Negotiation completes before any provider activation, so nothing has been invoked.
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L))

    [<Test>]
    member _.``PB-02 a contract version the endpoint does not recognize fails closed``() =
        let document =
            ContractCodec.encode CoolingFixture.contract |> withEntry "contractVersion" (CborInteger 2L)

        expectCategory ProtocolCategory.UnsupportedVersion (ContractCodec.decode document) |> ignore

    [<Test>]
    member _.``PB-03 an unmet required requirement fails closed``() =
        expectCategory
            ProtocolCategory.UnsupportedContract
            (negotiate CoolingFixture.contract (CoolingFixture.withoutProfileProvision ()))
        |> ignore

    [<Test>]
    member _.``PB-04 an opposed requirement that is offered is refused rather than ignored``() =
        expectCategory
            ProtocolCategory.UnsupportedContract
            (negotiate CoolingFixture.contract (CoolingFixture.withStreamingProvision ()))
        |> ignore

    [<Test>]
    member _.``PB-05 an undeclared contract field is refused before negotiation``() =
        let document =
            ContractCodec.encode CoolingFixture.contract |> withEntry "extension" (CborText "unknown")

        expectCategory ProtocolCategory.MalformedMessage (ContractCodec.decode document) |> ignore

    [<Test>]
    member _.``PB-06 an unknown enumeration value is refused rather than defaulted``() =
        let document = ContractCodec.encode CoolingFixture.contract
        let requirements = entry "requirements" document

        let mutated =
            match requirements with
            | CborArray(head :: tail) -> CborArray(withEntry "strength" (CborText "mandatory") head :: tail)
            | other -> other

        expectCategory
            ProtocolCategory.MalformedMessage
            (ContractCodec.decode (withEntry "requirements" mutated document))
        |> ignore

    [<Test>]
    member _.``PB-07 a compact identifier this binding never assigned resolves to no identity``() =
        let endpoint = establishedEndpoint ()

        expectCategory ProtocolCategory.UnsupportedContract (endpoint.ResolveOperation(OperationDesignation.Compact 4242))
        |> ignore

    [<Test>]
    member _.``PB-08 a name outside the portable profile is refused rather than accepted opaquely``() =
        assertAll (fun () ->
            // A Unicode-letter name is a valid Brontide canonical name but is not portable in 0.1.
            expectCategory ProtocolCategory.MalformedMessage (PortableName.tryCreate "interchange.tests.coöling")
            |> ignore

            expectCategory ProtocolCategory.MalformedMessage (PortableName.tryCreate "a:b:c") |> ignore
            expectCategory ProtocolCategory.MalformedMessage (PortableName.tryCreate "trailing.") |> ignore
            expectCategory ProtocolCategory.MalformedMessage (PortableCanonical.tryCreate "interchange.tests.ok" 0)
            |> ignore)

    [<Test>]
    member _.``PB-09 a request before the readiness signal is refused``() =
        let endpoint = establishedEndpoint ()

        let refused =
            endpoint.Request(
                ChannelRequestId.next (),
                OperationDesignation.Canonical CoolingFixture.setEnabled,
                CoolingFixture.commandV1,
                CoolingFixture.authorizedCommand "primary" true,
                []
            )

        expectCategory ProtocolCategory.StateViolation refused |> ignore

    [<Test>]
    member _.``PB-10 nested and repeated inline values conform and preserve order``() =
        let handler = CatalogHandler()
        let host = directHost CatalogFixture.contract handler

        let items =
            [ CatalogFixture.itemValue "a" "Alpha" [ "cold"; "cold"; "spare" ]
              CatalogFixture.itemValue "b" "Beta" [ "warm" ] ]

        let stored =
            invoke host CatalogFixture.upsert CatalogFixture.upsertCommand (CatalogFixture.upsertCommandValue items)

        Assert.That(stored.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)

        let found =
            invoke host CatalogFixture.find CatalogFixture.findCommand (CatalogFixture.findCommandValue [ "b"; "a" ])

        Assert.That(found.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)

        match found.Value |> Option.bind (PortableRecord.tryField "items") with
        | Some(PortableSequence [ first; second ]) ->
            // Sequence order is semantic: the answer follows the order the request asked in.
            Assert.That(PortableRecord.tryField "id" first, Is.EqualTo(Some(PortableText "b")))
            Assert.That(PortableRecord.tryField "id" second, Is.EqualTo(Some(PortableText "a")))

            match PortableRecord.tryField "tags" first with
            | Some(PortableSequence tags) -> Assert.That(List.length tags, Is.EqualTo 1)
            | _ -> Assert.Fail "The found item carries its declared tag sequence."
        | _ -> Assert.Fail "The find result carries the requested items in order."

    [<Test>]
    member _.``PB-11 an additive version difference is projected rather than refused``() =
        let host = directCoolingHost ()

        let command =
            CoolingFixture.command "primary" true None (Some "operator") (Some "supervisor")

        let result = invoke host CoolingFixture.setEnabled CoolingFixture.commandV2 command

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Accept)
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)
            Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo 1L))

    [<Test>]
    member _.``PB-12 a non-additive version difference refuses projection``() =
        let host = directCoolingHost ()

        let command =
            PortableRecord.ofFields
                [ "loop", PortableText "primary"
                  "enabled", PortableBoolean true
                  "reason", PortableText "maintenance" ]
            |> PortableRecord.withFragment CoolingFixture.hostContext [ "requesterLabel", PortableText "operator" ]

        let result = invoke host CoolingFixture.setEnabled CoolingFixture.commandV3 command

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Reject)
            Assert.That(result.Category, Is.EqualTo(Some ProtocolCategory.InvalidPayload))
            Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo 0L))

    [<Test>]
    member _.``PB-13 a closed result Shape refuses an undeclared Fragment rather than projecting it``() =
        let attaching =
            { new IPortableOperationHandler with
                member _.Invoke(_, _, _) =
                    Ok(
                        EffectSucceeded(
                            PortableRecord.ofFields
                                [ "loop", PortableText "primary"
                                  "coolingEnabled", PortableBoolean true
                                  "revision", PortableInteger 1L
                                  "providerEffectCount", PortableInteger 1L ]
                            |> PortableRecord.withFragment CoolingFixture.note [ "note", PortableText "extra" ],
                            1L
                        )
                    ) }

        let host = directHost CoolingFixture.contract attaching

        let result =
            invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 (CoolingFixture.authorizedCommand "primary" true)

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Reject)
            Assert.That(result.Category, Is.EqualTo(Some ProtocolCategory.InvalidPayload)))

    [<Test>]
    member _.``PB-14 a present-but-null optional field is refused``() =
        // Absence is expressed by omission, and null belongs only to unit.
        let encoded =
            CborArray
                [ CborMap
                      [ "enabled", CborBoolean true
                        "failureMode", CborNull
                        "loop", CborText "primary" ]
                  CborMap [] ]

        expectCategory
            ProtocolCategory.InvalidPayload
            (PortableValueCodec.decode catalog CoolingFixture.commandV1 [] encoded)
        |> ignore

    [<Test>]
    member _.``PB-15 an alternative outside the declared set is a payload decision``() =
        let encoded = CborArray [ CborText "quantity"; CborInteger 3L ]

        let fault =
            expectCategory
                ProtocolCategory.InvalidPayload
                (PortableValueCodec.decode catalog CoolingFixture.encodingChoice [] encoded)

        // The envelope kind was recognized, so this is invalid-payload rather than unsupported-kind.
        Assert.That(fault.LocalCode, Is.EqualTo "unknown-alternative")

    [<Test>]
    member _.``PB-16 exception-shaped data in a payload position is refused``() =
        let body =
            RequestBody.encode
                { Operation = OperationDesignation.Canonical CoolingFixture.setEnabled
                  InputShape = CoolingFixture.commandV1
                  Input = CborArray [ CborMap [ "stackTrace", CborText "at Provider.Invoke" ]; CborMap [] ]
                  Resources = [] }

        expectCategory ProtocolCategory.InvalidPayload (RequestBody.decode body) |> ignore

    [<Test>]
    member _.``PB-17 exception-shaped data in a control position is malformed``() =
        let envelope =
            EnvelopeCodec.toItem (Envelope.empty EnvelopeKind.Ready (ChannelId.next ()))
            |> withEntry "exception" (CborText "System.InvalidOperationException")

        expectCategory ProtocolCategory.MalformedMessage (EnvelopeCodec.ofItem envelope) |> ignore

    [<Test>]
    member _.``PB-57 a projection is recorded as a mapping obligation``() =
        let host = directCoolingHost ()

        let command =
            CoolingFixture.command "primary" true None (Some "operator") (Some "supervisor")

        let result = invoke host CoolingFixture.setEnabled CoolingFixture.commandV2 command

        shouldEqual
            [ $"projected:{PortableShapeRef.text CoolingFixture.commandV2}->{PortableShapeRef.text CoolingFixture.commandV1}" ]
            result.Observation.MappingObligations

        // The provider side records which structure the projection discarded, which is what
        // distinguishes a representation mapping from a semantic translation.
        let endpoint =
            PortableProviderEndpoint(CoolingFixture.contract, CoolingHandler(), Realization.FixedDirectCall)

        expectOk (endpoint.Establish(CoolingFixture.contract, "host")) |> ignore
        expectOk (endpoint.SignalReady())

        let outcome =
            expectOk (
                endpoint.Request(
                    ChannelRequestId.next (),
                    OperationDesignation.Canonical CoolingFixture.setEnabled,
                    CoolingFixture.commandV2,
                    command,
                    []
                )
            )

        Assert.That(outcome.MappingObligations, Contains.Item "field-projected:requestedBy")

    [<Test>]
    member _.``the limits declaration is refused when it is internally inconsistent``() =
        let inconsistent =
            { PortableLimits.declared with
                MaxResourceBytes = limits.MaxByteStringBytes + 1 }

        expectCategory ProtocolCategory.MalformedMessage (PortableLimits.validate inconsistent) |> ignore
