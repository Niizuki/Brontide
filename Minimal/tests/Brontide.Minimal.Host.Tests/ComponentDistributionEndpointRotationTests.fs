namespace Brontide.Minimal.Host.Tests

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi56Source(endpoint: ECDsa) =
    let mutable attempts = 0
    member _.Attempts = attempts
    interface IProviderPublisherTrustPolicyDistributionSource with
        member _.FetchAsync(request, _) =
            attempts <- attempts + 1
            let issued = 1800000000L
            let expires = issued + 60L
            let publicKey = endpoint.ExportSubjectPublicKeyInfo()
            let signature = endpoint.SignData(
                ProviderPublisherTrustPolicyDistributionManifest.encode
                    request.Challenge request.CurrentSequence request.CurrentPolicyIdentity issued expires None,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            Task.FromResult
                { Challenge = request.Challenge
                  CurrentSequence = request.CurrentSequence
                  CurrentPolicyIdentity = request.CurrentPolicyIdentity
                  IssuedAtUnixSeconds = issued
                  ExpiresAtUnixSeconds = expires
                  Update = None
                  Algorithm = "ECDSA-P256-SHA256"
                  EndpointPublicKeySpkiBase64 = Convert.ToBase64String publicKey
                  SignatureBase64 = Convert.ToBase64String signature }

[<TestFixture>]
type ComponentDistributionEndpointRotationTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI56 fixture value was missing." | present -> present

    let id (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyDistributionEndpointId.create

    let authorityId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let statement generation (previous: ECDsa) (next: ECDsa) (signer: ECDsa) (published: ECDsa) =
        let previousId, nextId = id previous, id next
        let signature = signer.SignData(
            ProviderDistributionEndpointRotationManifest.encode generation previousId nextId,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        { Generation = generation
          PreviousEndpoint = previousId
          NextEndpoint = nextId
          Algorithm = "ECDSA-P256-SHA256"
          PreviousEndpointPublicKeySpkiBase64 = Convert.ToBase64String(published.ExportSubjectPublicKeyInfo())
          SignatureBase64 = Convert.ToBase64String signature }

    let valid generation previous next = statement generation previous next previous previous
    let root () = Path.Combine(Path.GetTempPath(), $"brontide-cbi56-{Guid.NewGuid():N}")
    let openRotation path endpoint =
        DurableProviderDistributionEndpointRotation.Open(path, id endpoint, None).Rotation.Value
    let registry rootPath authority =
        let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(
            Path.Combine(rootPath, "policy.checkpoint"), authorityId authority, None)
        opened.Value
    let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L

    [<Test>]
    member _.``CBI56 C1 the durable anchor records initial active generation and stage``() =
        let rootPath = root ()
        try
            use a = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use b = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let path = Path.Combine(rootPath, "rotation.anchor")
            let opened = DurableProviderDistributionEndpointRotation.Open(path, id a, None)
            Assert.That(opened.Code, Is.EqualTo "endpoint-rotation-established")
            Assert.That(opened.Snapshot.Value.Generation, Is.Zero)
            Assert.That(opened.Snapshot.Value.ActiveEndpoint, Is.EqualTo(id a))
            Assert.That(opened.Rotation.Value.Stage(valid 1L a b).Code, Is.EqualTo "endpoint-rotation-staged")
            let recovered = DurableProviderDistributionEndpointRotation.Open(path, id a, None)
            Assert.That(recovered.Code, Is.EqualTo "endpoint-rotation-recovered")
            Assert.That(recovered.Snapshot.Value.StagedGeneration, Is.EqualTo(Some 1L))
            Assert.That(recovered.Snapshot.Value.StagedEndpoint, Is.EqualTo(Some(id b)))
        finally
            if Directory.Exists rootPath then Directory.Delete(rootPath, true)

    [<Test>]
    member _.``CBI56 C2 only the active endpoint can sign the exact successor``() =
        let rootPath = root ()
        try
            use a = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use b = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use c = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let rotation = openRotation (Path.Combine(rootPath, "rotation.anchor")) a
            Assert.That(rotation.Stage(statement 1L a b c a).Code, Is.EqualTo "endpoint-rotation-signature-invalid")
            Assert.That(rotation.Stage(statement 1L a b a c).Code, Is.EqualTo "endpoint-rotation-key-mismatch")
            Assert.That(rotation.Snapshot.StagedEndpoint, Is.EqualTo None)
        finally
            if Directory.Exists rootPath then Directory.Delete(rootPath, true)

    [<Test>]
    member _.``CBI56 C3 staging is strict durable and single successor``() =
        let rootPath = root ()
        try
            use a = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use b = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use c = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let rotation = openRotation (Path.Combine(rootPath, "rotation.anchor")) a
            Assert.That(rotation.Stage(valid 2L a b).Code, Is.EqualTo "endpoint-rotation-generation-invalid")
            Assert.That(rotation.Stage(valid 1L c b).Code, Is.EqualTo "endpoint-rotation-predecessor-mismatch")
            Assert.That(rotation.Stage(valid 1L a a).Code, Is.EqualTo "endpoint-rotation-self-refused")
            Assert.That(rotation.Stage(valid 1L a b).Code, Is.EqualTo "endpoint-rotation-staged")
            Assert.That(rotation.Stage(valid 1L a b).Code, Is.EqualTo "endpoint-rotation-already-staged")
            Assert.That(rotation.Stage(valid 1L a c).Code, Is.EqualTo "endpoint-rotation-successor-conflict")
        finally
            if Directory.Exists rootPath then Directory.Delete(rootPath, true)

    [<Test>]
    member _.``CBI56 C4 ordinary distribution remains pinned only to the active endpoint``() = task {
        let rootPath = root ()
        try
            use a = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use b = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let rotation = openRotation (Path.Combine(rootPath, "rotation.anchor")) a
            rotation.Stage(valid 1L a b) |> ignore
            let! actual = rotation.CreateCurrentClient(registry rootPath authority).SynchronizeAsync(
                Cbi56Source(b), now, TimeSpan.FromSeconds 1.0, CancellationToken.None)
            Assert.That(actual.Code, Is.EqualTo "policy-distribution-endpoint-mismatch")
        finally
            if Directory.Exists rootPath then Directory.Delete(rootPath, true)
    }

    [<Test>]
    member _.``CBI56 C5 confirmation uses one staged endpoint attempt and fails closed``() = task {
        let rootPath = root ()
        try
            use a = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use b = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let rotation = openRotation (Path.Combine(rootPath, "rotation.anchor")) a
            rotation.Stage(valid 1L a b) |> ignore
            let source = Cbi56Source(a)
            let! actual = rotation.ConfirmAsync(registry rootPath authority, source, now,
                TimeSpan.FromSeconds 1.0, CancellationToken.None)
            Assert.That(actual.Code, Is.EqualTo "endpoint-rotation-confirmation-refused")
            Assert.That(actual.DistributionCode, Is.EqualTo "policy-distribution-endpoint-mismatch")
            Assert.That(actual.Snapshot.ActiveEndpoint, Is.EqualTo(id a))
            Assert.That(actual.Snapshot.StagedEndpoint, Is.EqualTo(Some(id b)))
            Assert.That(source.Attempts, Is.EqualTo 1)
        finally
            if Directory.Exists rootPath then Directory.Delete(rootPath, true)
    }

    [<Test>]
    member _.``CBI56 C6 activation is durable only after successful confirmation``() = task {
        let rootPath = root ()
        try
            use a = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use b = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let path = Path.Combine(rootPath, "rotation.anchor")
            let rotation = openRotation path a
            rotation.Stage(valid 1L a b) |> ignore
            let! actual = rotation.ConfirmAsync(registry rootPath authority, Cbi56Source(b), now,
                TimeSpan.FromSeconds 1.0, CancellationToken.None)
            let recovered = DurableProviderDistributionEndpointRotation.Open(path, id a, None)
            Assert.That(actual.Code, Is.EqualTo "endpoint-rotation-applied")
            Assert.That(actual.DistributionCode, Is.EqualTo "policy-distribution-current")
            Assert.That(recovered.Snapshot.Value.Generation, Is.EqualTo 1L)
            Assert.That(recovered.Snapshot.Value.ActiveEndpoint, Is.EqualTo(id b))
            Assert.That(recovered.Snapshot.Value.StagedEndpoint, Is.EqualTo None)

            let failedPath = Path.Combine(rootPath, "failed-rotation.anchor")
            let failed = openRotation failedPath a
            failed.Stage(valid 1L a b) |> ignore
            Directory.CreateDirectory(failedPath + ".tmp") |> ignore
            let! writeFailure = failed.ConfirmAsync(registry rootPath authority, Cbi56Source(b), now,
                TimeSpan.FromSeconds 1.0, CancellationToken.None)
            Assert.That(writeFailure.Code, Is.EqualTo "endpoint-rotation-write-failed")
            Assert.That(writeFailure.DistributionCode, Is.EqualTo "policy-distribution-current")
            Assert.That(writeFailure.Snapshot.ActiveEndpoint, Is.EqualTo(id a))
            Assert.That(writeFailure.Snapshot.StagedEndpoint, Is.EqualTo(Some(id b)))
        finally
            if Directory.Exists rootPath then Directory.Delete(rootPath, true)
    }

    [<Test>]
    member _.``CBI56 C7 recovery rejects damage and rollback below the retained floor``() = task {
        let rootPath = root ()
        try
            use a = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use b = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let path = Path.Combine(rootPath, "rotation.anchor")
            let rotation = openRotation path a
            let initialBytes = File.ReadAllBytes path
            rotation.Stage(valid 1L a b) |> ignore
            let! applied = rotation.ConfirmAsync(registry rootPath authority, Cbi56Source(b), now,
                TimeSpan.FromSeconds 1.0, CancellationToken.None)
            File.WriteAllBytes(path, initialBytes)
            Assert.That(DurableProviderDistributionEndpointRotation.Open(path, id a, Some applied.Floor).Code,
                Is.EqualTo "endpoint-rotation-rollback-detected")
            let damaged = Array.copy initialBytes
            damaged[0] <- damaged[0] ^^^ 1uy
            File.WriteAllBytes(path, damaged)
            Assert.That(DurableProviderDistributionEndpointRotation.Open(path, id a, None).Code,
                Is.EqualTo "endpoint-rotation-corrupt")
        finally
            if Directory.Exists rootPath then Directory.Delete(rootPath, true)
    }

    [<Test>]
    member _.``CBI56 C8 shared vectors pin portable rotation observations``() =
        use fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi56-distribution-endpoint-rotation-vectors.json")))
        for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
            let rootPath = root ()
            try
                use a = ECDsa.Create(ECCurve.NamedCurves.nistP256)
                use b = ECDsa.Create(ECCurve.NamedCurves.nistP256)
                use c = ECDsa.Create(ECCurve.NamedCurves.nistP256)
                let keys = Dictionary<string, ECDsa>()
                keys.Add("A", a); keys.Add("B", b); keys.Add("C", c)
                let previous = keys[vector.GetProperty("previous").GetString() |> required]
                let next = keys[vector.GetProperty("next").GetString() |> required]
                let evidence = vector.GetProperty("evidence").GetString() |> required
                let signer = if evidence = "wrong-signer" then c else previous
                let published = if evidence = "wrong-public-key" then c else previous
                let rotation = openRotation (Path.Combine(rootPath, "rotation.anchor")) a
                let actual = rotation.Stage(
                    statement (vector.GetProperty("generation").GetInt64()) previous next signer published)
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()),
                    vector.GetProperty("name").GetString())
            finally
                if Directory.Exists rootPath then Directory.Delete(rootPath, true)
