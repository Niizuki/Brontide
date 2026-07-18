namespace Brontide.Minimal.Interchange.Tests

open System
open System.IO
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel
open Brontide.Minimal.Binding
open Brontide.Minimal.Experimental.Enrichment
open Brontide.Minimal.Vocabularies.Cooling

module private Helpers =
    let wire name version = { Name = name; Version = version }

    let manifest implementation =
        { ProtocolVersion = 1
          Implementation = implementation
          Shapes = Set.ofList [ wire "brontide-minimal.cooling.state" 1 ]
          Operations = Set.ofList [ wire "brontide-minimal.cooling.apply" 1 ] }

    let fixture parts =
        Array.append [| TestContext.CurrentContext.TestDirectory; "interchange" |] parts
        |> Path.Combine

    let portableLaunch variable arguments =
        match Environment.GetEnvironmentVariable variable |> Option.ofObj with
        | Some path when File.Exists path ->
            { FileName = path
              Arguments = String.concat " " arguments }
        | _ ->
            Assert.Ignore($"{variable} does not name a built provider endpoint.")
            failwith "The cross-process test was ignored."

    let errorMessage = function
        | Error message -> message
        | Ok _ -> ""

open Helpers

[<TestFixture>]
type InterchangeTests() =
    [<Test>]
    member _.``external manifests negotiate data contracts without shared CLR types`` () =
        let local = manifest "brontide-minimal-fsharp"
        let remote = manifest "external-runtime"

        let negotiated =
            Manifest.negotiate local.Shapes local.Operations local remote
            |> Result.defaultWith failwith

        Assert.That(negotiated.RemoteImplementation, Is.EqualTo "external-runtime")
        Assert.That(Set.count negotiated.Shapes, Is.EqualTo 1)
        Assert.That(Set.count negotiated.Operations, Is.EqualTo 1)

    [<Test>]
    member _.``protocol mismatches fail before value exchange`` () =
        let local = manifest "brontide-minimal-fsharp"
        let remote = { manifest "external-runtime" with ProtocolVersion = 2 }

        Assert.That(Manifest.negotiate Set.empty Set.empty local remote |> Result.isError, Is.True)

    [<Test>]
    member _.``manifests have a JSON round trip`` () =
        let source = manifest "brontide-minimal-fsharp"
        let decoded = Manifest.toJson source |> Manifest.tryFromJson |> Result.defaultWith failwith

        Assert.That(decoded.Implementation, Is.EqualTo source.Implementation)
        Assert.That(Set.count decoded.Shapes, Is.EqualTo(Set.count source.Shapes))

    [<Test>]
    member _.``ShapeValue codec round trips nested values and fragments`` () =
        let fragment: FragmentReference =
            { Name = CanonicalName.create "brontide-minimal.tests.fragment"
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

    [<Test>]
    member _.``neutral version two manifest and golden values parse independently`` () =
        let manifestJson = File.ReadAllText(fixture [| "manifest-v2.json" |])

        let decoded =
            PortableManifestCodec.tryFromJson manifestJson
            |> Result.defaultWith failwith

        let roundTripped =
            PortableManifestCodec.toJson decoded
            |> PortableManifestCodec.tryFromJson
            |> Result.defaultWith failwith

        let value name =
            File.ReadAllText(fixture [| "values"; name |])
            |> PortableValueCodec.decode
            |> Result.defaultWith failwith

        let valid = value "valid-command.json"
        let optional = value "valid-command-with-optional-fragment.json"
        let missing = value "invalid-command-missing-fragment.json"
        let wrongKind = value "invalid-command-wrong-kind.json"

        Assert.That(roundTripped.ProtocolVersion, Is.EqualTo 2)
        Assert.That(roundTripped.Component.Name, Is.EqualTo "interchange.tests.cooling-component")
        Assert.That(PortableContract.validateCommand valid |> Result.isOk, Is.True)
        Assert.That(PortableContract.validateCommand optional |> Result.isOk, Is.True)
        Assert.That(
            PortableContract.validateCommand missing |> errorMessage |> String.IsNullOrEmpty,
            Is.False
        )
        Assert.That(
            PortableContract.validateCommand wrongKind |> errorMessage,
            Does.Contain "wrong kind"
        )

    [<Test>]
    member _.``portable value parser rejects private metadata duplicate fields and exception-shaped data`` () =
        let privateType = File.ReadAllText(fixture [| "values"; "invalid-private-type.json" |])

        let duplicate =
            "{\"kind\":\"record\",\"kind\":\"record\",\"fields\":{},\"fragments\":{}}"

        let exceptionValue =
            "{\"kind\":\"record\",\"fields\":{\"exception\":{\"kind\":\"text\",\"value\":\"bad\"}},\"fragments\":{}}"

        Assert.That(PortableValueCodec.decode privateType |> Result.isError, Is.True)
        Assert.That(PortableValueCodec.decode duplicate |> Result.isError, Is.True)
        Assert.That(PortableValueCodec.decode exceptionValue |> Result.isError, Is.True)

[<TestFixture>]
[<Category("CrossProcess")>]
type MinimalHostsReferenceTests() =
    let referenceProvider arguments = portableLaunch "BRONTIDE_REFERENCE_PROVIDER" arguments

    [<Test>]
    member _.``compatible activation success forwarding provenance and host Enrichment are visible`` () =
        let host = MinimalCoolingBindingHost(referenceProvider [], TimeProvider.System)

        let declaration: TargetedEnrichmentDeclaration =
            { Name = CanonicalName.create "brontide-minimal.interchange.host-context"
              Target = host.Operation
              Fragment = host.HostContext
              RequiredSources = Set.singleton "requester"
              Derive =
                fun sources ->
                    match sources["requester"] with
                    | TextValue requester ->
                        Ok(RecordValue(Map.ofList [ "requesterLabel", TextValue requester ], Map.empty))
                    | _ -> Error "The requester label must be text." }

        let baseInput =
            PortableContract.command "primary" true None None (Some "preserve exactly")

        let available =
            { Key = "requester"
              Value = TextValue "brontide-minimal-requester"
              Provenance = "host-local actor label" }

        let enriched =
            TargetedEnrichment.resolve
                host.Operation
                host.HostContext
                declaration
                [ available ]
                baseInput
            |> Result.defaultWith failwith

        let result = host.Execute(host.AuthorizedActor.Reference, enriched.Input)

        let coolingEnabled =
            match result.Step.Outcome.Result with
            | Some(RecordValue(fields, _)) -> fields["coolingEnabled"]
            | _ -> failwith "The Brontide.Reference provider did not return a Cooling result."

        let forwardedFragments =
            match result.ForwardedInput with
            | Some(RecordValue(_, fragments)) -> fragments
            | _ -> failwith "The binding did not preserve the forwarded input."

        Assert.That(result.Step.Outcome.Status, Is.EqualTo Succeeded)
        Assert.That(coolingEnabled, Is.EqualTo(BooleanValue true))
        Assert.That(result.Observation.SelectedProvider, Is.EqualTo "brontide-reference-csharp-provider")
        Assert.That(result.Observation.HostAuthorityDecision, Is.EqualTo "allowed")
        Assert.That(result.Observation.CrossedBoundaries, Does.Contain "process")
        Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(Some 1L))
        Assert.That(
            BindingRequestId.value result.Observation.RequestId,
            Is.Not.EqualTo(BindingExecutionId.value result.Observation.BindingExecutionId)
        )
        Assert.That(Map.containsKey host.OptionalForwardingNote forwardedFragments, Is.True)
        Assert.That(enriched.Sources["requester"], Is.EqualTo "host-local actor label")

    [<Test>]
    member _.``authority unknown Constraint and missing Fragment stop before provider effect`` () =
        let deniedHost = MinimalCoolingBindingHost(referenceProvider [], TimeProvider.System)

        let denied =
            deniedHost.Execute(
                deniedHost.DeniedActor.Reference,
                PortableContract.command "primary" true None (Some "denied") None
            )

        let unknownHost =
            MinimalCoolingBindingHost(
                referenceProvider [],
                TimeProvider.System,
                requireUnknownConstraint = true
            )

        let unknown =
            unknownHost.Execute(
                unknownHost.AuthorizedActor.Reference,
                PortableContract.command "primary" true None (Some "unknown") None
            )

        let fragmentHost = MinimalCoolingBindingHost(referenceProvider [], TimeProvider.System)

        let missing =
            fragmentHost.Execute(
                fragmentHost.AuthorizedActor.Reference,
                PortableContract.command "primary" true None None None
            )

        Assert.That(denied.Step.Outcome.Status, Is.EqualTo Denied)
        Assert.That(unknown.Step.Outcome.Status, Is.EqualTo Denied)
        Assert.That(missing.Step.Outcome.Status, Is.EqualTo Failed)
        Assert.That(deniedHost.ProviderStarts, Is.Zero)
        Assert.That(unknownHost.ProviderStarts, Is.Zero)
        Assert.That(fragmentHost.ProviderStarts, Is.Zero)
        Assert.That(unknown.Step.Outcome.Reason.Value, Does.Contain "no evaluator")
        Assert.That(missing.Step.Outcome.Reason.Value, Does.Contain "required fragment")

    [<Test>]
    member _.``semantic failure crosses as a failed Outcome with shaped details and no exception`` () =
        let host = MinimalCoolingBindingHost(referenceProvider [], TimeProvider.System)

        let result =
            host.Execute(
                host.AuthorizedActor.Reference,
                PortableContract.command
                    "primary"
                    true
                    (Some "semantic")
                    (Some "brontide-minimal-requester")
                    None
            )

        let code =
            match result.ProviderDetails with
            | Some(RecordValue(fields, _)) -> fields["code"]
            | _ -> failwith "The failed provider details were not retained."

        Assert.That(result.Step.Outcome.Status, Is.EqualTo Failed)
        Assert.That(code, Is.EqualTo(TextValue "requested-failure"))
        Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(Some 0L))
        Assert.That(result.Step.Outcome.Reason.Value, Does.Not.Contain "Exception")

    [<Test>]
    member _.``incompatible manifests fail before activation and name missing Operations and Shapes`` () =
        let removeShape (manifest: PortableManifest) =
            { manifest with
                Shapes =
                    manifest.Shapes
                    |> List.filter (fun (shape: PortableShape) ->
                        shape.Reference <> PortableContract.resultShape) }

        let shapeHost =
            MinimalCoolingBindingHost(
                referenceProvider [],
                TimeProvider.System,
                manifestTransform = removeShape
            )

        let missingShape =
            shapeHost.Execute(
                shapeHost.AuthorizedActor.Reference,
                PortableContract.command "primary" true None (Some "brontide-minimal-requester") None
            )

        let removeOperation (manifest: PortableManifest) = { manifest with Operations = [] }

        let operationHost =
            MinimalCoolingBindingHost(
                referenceProvider [],
                TimeProvider.System,
                manifestTransform = removeOperation
            )

        let missingOperation =
            operationHost.Execute(
                operationHost.AuthorizedActor.Reference,
                PortableContract.command "primary" true None (Some "brontide-minimal-requester") None
            )

        Assert.That(missingShape.Step.Outcome.Status, Is.EqualTo Failed)
        Assert.That(missingShape.Observation.FailureDomain, Is.EqualTo "binding-negotiation")
        Assert.That(missingShape.Step.Outcome.Reason.Value, Does.Contain "Shape")
        Assert.That(missingShape.Observation.ProviderEffectCount, Is.EqualTo None)
        Assert.That(missingOperation.Step.Outcome.Status, Is.EqualTo Failed)
        Assert.That(missingOperation.Observation.FailureDomain, Is.EqualTo "binding-negotiation")
        Assert.That(missingOperation.Step.Outcome.Reason.Value, Does.Contain "Operation")
        Assert.That(missingOperation.Observation.ProviderEffectCount, Is.EqualTo None)

    [<Test>]
    member _.``protocol and provider process failures are explicit and never fabricate success`` () =
        let protocolHost =
            MinimalCoolingBindingHost(
                referenceProvider [ "--reject-protocol" ],
                TimeProvider.System
            )

        let protocol =
            protocolHost.Execute(
                protocolHost.AuthorizedActor.Reference,
                PortableContract.command "primary" true None (Some "brontide-minimal-requester") None
            )

        let crashHost =
            MinimalCoolingBindingHost(
                referenceProvider [ "--crash-after-activation" ],
                TimeProvider.System
            )

        let crashed =
            crashHost.Execute(
                crashHost.AuthorizedActor.Reference,
                PortableContract.command "primary" true None (Some "brontide-minimal-requester") None
            )

        Assert.That(protocol.Step.Outcome.Status, Is.EqualTo Failed)
        Assert.That(protocol.Observation.FailureDomain, Is.EqualTo "binding-negotiation")
        Assert.That(crashed.Step.Outcome.Status, Is.EqualTo Failed)
        Assert.That(crashed.Observation.ProviderProcessFailure, Is.True)
        Assert.That(crashed.Observation.Interrupted, Is.True)
        Assert.That(crashed.Observation.RetryCount, Is.Zero)
        Assert.That(crashed.Observation.Fallback, Is.EqualTo "none")
