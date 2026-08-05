namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi57Observation = { Code: string; Generation: int64; BytesChanged: bool }

[<TestFixture>]
type ComponentPolicyAuthorityRotationTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI57 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi57-policy-authority-rotation-vectors.json")))

    let authorityId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let policy revoked : ProviderPublisherTrustPolicy =
        let entries = [ { PublisherKeyId = ProviderPublisherKeyId.create (String('A', 64))
                          Disposition = if revoked then Revoked else Admitted } ]
        { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }

    let signUpdate (key: ECDsa) sequence previous (value: ProviderPublisherTrustPolicy) =
        { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = value
          Algorithm = "ECDSA-P256-SHA256"
          AuthorityPublicKeySpkiBase64 = key.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          SignatureBase64 =
            key.SignData(
                ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous value.Identity,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String }: ProviderPublisherTrustPolicyUpdate

    /// Builds a rotation statement. A statement whose generation is not a transition at all is still
    /// signed over a well-formed manifest, so its refusal stays attributable to the generation rather
    /// than to bytes nobody would have verified.
    let statement generation policySequence policyIdentity (previous: ECDsa) (next: ECDsa) evidence (other: ECDsa) =
        let previousId = authorityId previous
        let nextId = authorityId next
        let manifest =
            ProviderPolicyAuthorityRotationManifest.encode (max generation 1L) policySequence policyIdentity
                previousId nextId
        let sign (key: ECDsa) =
            key.SignData(manifest, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        let publish (key: ECDsa) = key.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
        { Generation = generation
          PolicySequence = policySequence
          PolicyIdentity = policyIdentity
          PreviousAuthority = previousId
          NextAuthority = nextId
          Algorithm = if evidence = "algorithm" then "RSA-PSS-SHA256" else "ECDSA-P256-SHA256"
          PreviousAuthorityPublicKeySpkiBase64 =
            publish (if evidence = "wrong-predecessor-key" then other else previous)
          NextAuthorityPublicKeySpkiBase64 =
            publish (if evidence = "wrong-successor-key" then other else next)
          PreviousSignatureBase64 = sign (if evidence = "wrong-predecessor-signer" then other else previous)
          NextSignatureBase64 = sign (if evidence = "wrong-successor-signer" then other else next) }
        : ProviderPolicyAuthorityRotationStatement

    let valid generation policySequence policyIdentity previous next (other: ECDsa) =
        statement generation policySequence policyIdentity previous next "valid" other

    let openAt path authority = DurableProviderPublisherTrustPolicyRegistry.Open(path, authority, None)

    let newRoot name = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-{name}-{Guid.NewGuid():N}")

    let withRoot name action =
        let root = newRoot name
        try action root (Path.Combine(root, "policy.checkpoint"))
        finally if Directory.Exists root then Directory.Delete(root, true)

    let run (vector: JsonElement) =
        withRoot "vector" (fun _ path ->
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let keys = Map.ofList [ "A", a; "B", b; "C", c ]
            let _, opened, _ = openAt path (authorityId a)
            let durable = opened.Value
            let initial = policy false
            Assert.That(durable.Apply(signUpdate a 1L None initial).IsApplied, Is.True)
            let before = File.ReadAllBytes path
            let result =
                durable.Rotate(statement
                    (vector.GetProperty("generation").GetInt64())
                    (vector.GetProperty("policySequence").GetInt64())
                    (match vector.GetProperty("policyIdentity").GetString() |> required with
                     | "current" -> Some initial.Identity
                     | "other" -> Some(ProviderPublisherTrustPolicyId.create (String('0', 64)))
                     | _ -> None)
                    keys[vector.GetProperty("previous").GetString() |> required]
                    keys[vector.GetProperty("next").GetString() |> required]
                    (vector.GetProperty("evidence").GetString() |> required)
                    c)
            { Code = result.Code
              Generation = result.Generation
              BytesChanged = File.ReadAllBytes path <> before })

    [<Test>]
    member _.``shared CBI57 vectors rotate only on a countersigned successor of the active authority``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let actual = run vector
            Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()),
                        vector.GetProperty("name").GetString())

    [<Test>]
    member _.``CBI57 C1 the pin is immutable and the active authority is derived from it``() =
        withRoot "pin" (fun _ path ->
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _, authorityFloor =
                DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId a, None, None)
            let durable = opened.Value
            let initial = policy false
            durable.Apply(signUpdate a 1L None initial) |> ignore
            Assert.Multiple(Action(fun () ->
                Assert.That(authorityFloor.Value.Generation, Is.Zero)
                Assert.That(durable.ActiveAuthorityIdentity, Is.EqualTo(authorityId a))))
            let rotated = durable.Rotate(valid 1L 1L (Some initial.Identity) a b c)

            let code, recovered, _, recoveredFloor =
                DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId a, None, None)
            Assert.Multiple(Action(fun () ->
                Assert.That(rotated.Code, Is.EqualTo "policy-authority-rotation-applied")
                Assert.That(code, Is.EqualTo "policy-checkpoint-recovered")
                Assert.That(recovered.Value.AuthorityIdentity, Is.EqualTo(authorityId a),
                            "the stored pin is never rewritten by a rotation")
                Assert.That(recovered.Value.ActiveAuthorityIdentity, Is.EqualTo(authorityId b))
                Assert.That(recovered.Value.AuthorityGeneration, Is.EqualTo 1L)
                Assert.That(recoveredFloor.Value.ActiveAuthority, Is.EqualTo(authorityId b))))

            // The successor is not a pin. Opening the same record as though B had been pinned out of
            // band is refused, because the chain it must be verified from starts at A.
            let mismatch, _, _ = openAt path (authorityId b)
            Assert.That(mismatch, Is.EqualTo "policy-checkpoint-authority-mismatch"))

    [<Test>]
    member _.``CBI57 C2 a successor must be authorized and countersigned over the same manifest``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty("vectors").EnumerateArray() |> Seq.toList
        Assert.Multiple(Action(fun () ->
            for name in [ "predecessor-signature-invalid"; "successor-unproven"
                          "predecessor-key-mismatch"; "successor-key-mismatch"; "unsupported-algorithm" ] do
                let vector =
                    vectors |> List.find (fun item -> item.GetProperty("name").GetString() = name)
                let actual = run vector
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), name)
                Assert.That(actual.Generation, Is.Zero, name)))

        // A countersignature is bound to the transition it accepts: B's signature over generation 1
        // does not carry to generation 2, which is what covering one manifest with both keys buys.
        withRoot "bind" (fun _ path ->
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = openAt path (authorityId a)
            let durable = opened.Value
            Assert.That(durable.Rotate(valid 1L 0L None a b c).IsApplied, Is.True)
            let lifted = valid 1L 0L None b c a
            let replayed = durable.Rotate { lifted with Generation = 2L }
            Assert.Multiple(Action(fun () ->
                Assert.That(replayed.Code, Is.EqualTo "policy-authority-signature-invalid")
                Assert.That(durable.ActiveAuthorityIdentity, Is.EqualTo(authorityId b)))))

    [<Test>]
    member _.``CBI57 C3 a rotation is one atomic link and nothing is ever staged``() =
        use document = fixture ()
        Assert.Multiple(Action(fun () ->
            for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
                let actual = run vector
                let applied = vector.GetProperty("code").GetString() = "policy-authority-rotation-applied"
                let label = vector.GetProperty("name").GetString()
                Assert.That(actual.BytesChanged, Is.EqualTo applied, label)
                Assert.That(actual.Generation, Is.EqualTo(if applied then 1L else 0L), label)))

        // The absent phase is the contract: unlike CBI56 there is no staged successor to announce,
        // confirm, or abandon, because a countersignature already proves what a network attempt would.
        let staging =
            typeof<DurableProviderPublisherTrustPolicyRegistry>.GetMembers()
            |> Array.filter (fun member' -> member'.Name.Contains "Stage" || member'.Name.Contains "Confirm")
        Assert.That(staging, Is.Empty)

        withRoot "write" (fun root path ->
            Directory.CreateDirectory root |> ignore
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = openAt path (authorityId a)
            let durable = opened.Value
            Directory.CreateDirectory(path + ".tmp") |> ignore
            let failed = durable.Rotate(valid 1L 0L None a b c)
            Assert.Multiple(Action(fun () ->
                Assert.That(failed.Code, Is.EqualTo "policy-checkpoint-write-failed")
                Assert.That(failed.ActiveAuthority, Is.EqualTo(authorityId a))
                Assert.That(durable.AuthorityGeneration, Is.Zero))))

    [<Test>]
    member _.``CBI57 C4 retirement is immediate and the predecessor's history stays verifiable``() =
        withRoot "retire" (fun _ path ->
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = openAt path (authorityId a)
            let durable = opened.Value
            let initial = policy false
            let successor = policy true
            durable.Apply(signUpdate a 1L None initial) |> ignore
            Assert.That(durable.Rotate(valid 1L 1L (Some initial.Identity) a b c).IsApplied, Is.True)

            Assert.Multiple(Action(fun () ->
                Assert.That(durable.Apply(signUpdate a 2L (Some initial.Identity) successor).Code,
                            Is.EqualTo "policy-update-authority-mismatch",
                            "the retired predecessor can sign nothing further")
                Assert.That(durable.Apply(signUpdate b 2L (Some initial.Identity) successor).IsApplied, Is.True)))

            let code, recovered, _ = openAt path (authorityId a)
            Assert.Multiple(Action(fun () ->
                Assert.That(code, Is.EqualTo "policy-checkpoint-recovered",
                            "the predecessor's own update is re-verified as its work")
                Assert.That(recovered.Value.Current.Value.Sequence, Is.EqualTo 2L)))

            // An update cannot precede the rotation that authorized its signer, which is the order the
            // retained chain states and the reason a rotation is a link rather than a side record.
            let live = ProviderPublisherTrustPolicyRegistry(authorityId a)
            Assert.That((live.Apply(signUpdate b 1L None initial)).Code,
                        Is.EqualTo "policy-update-authority-mismatch"))

    [<Test>]
    member _.``CBI57 C5 the record advances its format only when a rotation exists``() =
        withRoot "record" (fun _ path ->
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = openAt path (authorityId a)
            let durable = opened.Value
            let initial = policy false
            durable.Apply(signUpdate a 1L None initial) |> ignore
            let updatesOnly = File.ReadAllBytes path
            Assert.That(Encoding.UTF8.GetString(updatesOnly, 4, 5), Is.EqualTo "CBI38",
                        "a host that never rotates keeps the record shape CBI38 wrote")
            let rotation = valid 1L 1L (Some initial.Identity) a b c
            Assert.That(durable.Rotate(rotation).IsApplied, Is.True)
            let rotated = File.ReadAllBytes path
            Assert.That(Encoding.UTF8.GetString(rotated, 4, 5), Is.EqualTo "CBI57")

            // Damage to the retained rotation is refused rather than replayed: recovery re-verifies the
            // transition instead of trusting that it was verified once.
            let damaged = Array.copy rotated
            let signature = Encoding.UTF8.GetBytes rotation.NextSignatureBase64
            let offset =
                seq { 0 .. damaged.Length - signature.Length }
                |> Seq.tryFind (fun index -> damaged.AsSpan(index, signature.Length).SequenceEqual(signature))
            Assert.That(offset.IsSome, Is.True)
            damaged[offset.Value] <- if damaged[offset.Value] = byte 'A' then byte 'B' else byte 'A'
            File.WriteAllBytes(path, damaged)
            let damagedCode, _, _ = openAt path (authorityId a)
            Assert.That(damagedCode, Is.EqualTo "policy-checkpoint-invalid-chain")

            // An unknown link tag is refused by decoding. The tag is the first int32 after the format
            // marker, the pinned authority, and the link count.
            let unknownTag = Array.copy rotated
            unknownTag[4 + 5 + 4 + 64 + 4 + 3] <- 2uy
            File.WriteAllBytes(path, unknownTag)
            let unknownCode, _, _ = openAt path (authorityId a)
            Assert.That(unknownCode, Is.EqualTo "policy-checkpoint-corrupt")

            File.WriteAllBytes(path, Array.append rotated [| 0uy |])
            let trailingCode, _, _ = openAt path (authorityId a)
            Assert.That(trailingCode, Is.EqualTo "policy-checkpoint-corrupt")

            File.WriteAllBytes(path, rotated[.. rotated.Length / 2 - 1])
            let truncatedCode, _, _ = openAt path (authorityId a)
            Assert.That(truncatedCode, Is.EqualTo "policy-checkpoint-corrupt"))

    [<Test>]
    member _.``CBI57 C6 an external authority floor detects rotation rollback``() =
        withRoot "floor" (fun _ path ->
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = openAt path (authorityId a)
            let durable = opened.Value
            let initial = policy false
            durable.Apply(signUpdate a 1L None initial) |> ignore
            let beforeRotation = File.ReadAllBytes path
            let rotated = durable.Rotate(valid 1L 1L (Some initial.Identity) a b c)
            let floor = rotated.Floor
            Assert.That(floor.Generation, Is.EqualTo 1L)

            File.WriteAllBytes(path, beforeRotation)
            let rollback, _, _, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId a, None, Some floor)
            let ownFloor, _, _, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(
                    path, authorityId a, None, Some(ProviderPolicyAuthorityFloor.Restore(0L, authorityId a)))
            Assert.Multiple(Action(fun () ->
                Assert.That(rollback, Is.EqualTo "policy-authority-rollback-detected")
                Assert.That(ownFloor, Is.EqualTo "policy-checkpoint-recovered",
                            "the pre-rotation record is exactly what its own floor describes")))

            // An equal generation reached under a different successor is a conflict rather than a
            // rollback of sequence, and it fails closed the same way.
            File.Delete path
            let _, second, _ = openAt path (authorityId a)
            second.Value.Apply(signUpdate a 1L None initial) |> ignore
            second.Value.Rotate(valid 1L 1L (Some initial.Identity) a c b) |> ignore
            let conflict, _, _, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId a, None, Some floor)
            let matching, _, _, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(
                    path, authorityId a, None, Some(ProviderPolicyAuthorityFloor.Restore(1L, authorityId c)))
            Assert.Multiple(Action(fun () ->
                Assert.That(conflict, Is.EqualTo "policy-authority-rollback-detected")
                Assert.That(matching, Is.EqualTo "policy-checkpoint-recovered")))

            // A deleted record cannot satisfy a floor that has seen a rotation.
            File.Delete path
            let missing, _, _, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId a, None, Some floor)
            Assert.That(missing, Is.EqualTo "policy-authority-rollback-detected"))

    [<Test>]
    member _.``CBI57 C7 a rotation moves no policy disposition or compared identity``() =
        withRoot "trust" (fun _ path ->
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = openAt path (authorityId a)
            let durable = opened.Value
            let initial = policy false
            durable.Apply(signUpdate a 1L None initial) |> ignore
            let before = durable.Current.Value
            Assert.That(durable.Rotate(valid 1L 1L (Some initial.Identity) a b c).IsApplied, Is.True)
            let after = durable.Current.Value
            Assert.Multiple(Action(fun () ->
                Assert.That(after.Policy.Identity, Is.EqualTo before.Policy.Identity)
                Assert.That(after.Policy.Entries, Is.EqualTo(before.Policy.Entries :> obj))
                Assert.That(after.Sequence, Is.EqualTo before.Sequence)
                Assert.That(durable.AuthorityIdentity, Is.EqualTo(authorityId a))
                Assert.That(durable.ActiveAuthorityIdentity, Is.EqualTo(authorityId b))))

            // The rotation leaves the snapshot untouched, so comparing it with itself would prove
            // nothing. What has to hold is that the next snapshot — verified under the successor — still
            // names the trust root, because that is the identity CBI44's launch decision and CBI45's
            // serving revalidation compare against what a launch recorded.
            let succeeded = durable.Apply(signUpdate b 2L (Some initial.Identity) (policy true))
            Assert.Multiple(Action(fun () ->
                Assert.That(succeeded.IsApplied, Is.True)
                Assert.That(succeeded.Current.Value.AuthorityIdentity, Is.EqualTo(authorityId a)))))

    [<Test>]
    member _.``CBI57 C8 a rotation before any policy exists is an ordinary transition``() =
        withRoot "empty" (fun _ path ->
            use a = ECDsa.Create ECCurve.NamedCurves.nistP256
            use b = ECDsa.Create ECCurve.NamedCurves.nistP256
            use c = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = openAt path (authorityId a)
            let durable = opened.Value
            Assert.That(durable.Rotate(valid 1L 0L None a b c).IsApplied, Is.True)
            let initial = policy false
            Assert.Multiple(Action(fun () ->
                Assert.That(durable.Apply(signUpdate a 1L None initial).Code,
                            Is.EqualTo "policy-update-authority-mismatch")
                Assert.That(durable.Apply(signUpdate b 1L None initial).IsApplied, Is.True)))
            let code, recovered, _ = openAt path (authorityId a)
            Assert.Multiple(Action(fun () ->
                Assert.That(code, Is.EqualTo "policy-checkpoint-recovered")
                Assert.That(recovered.Value.Current.Value.Sequence, Is.EqualTo 1L)
                Assert.That(recovered.Value.AuthorityGeneration, Is.EqualTo 1L))))
