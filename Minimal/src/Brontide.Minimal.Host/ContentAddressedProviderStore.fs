namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text
open Brontide.Minimal.Binding.Portable

[<Struct; StructuralEquality; StructuralComparison>]
type ProviderArtifactSetId = private ProviderArtifactSetId of string

[<RequireQualifiedAccess>]
module ProviderArtifactSetId =
    let private valid (value: string) =
        value.Length = 64
        && value |> Seq.forall (fun character ->
            (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))

    let create (value: string) =
        if String.IsNullOrEmpty value || not (valid value) then
            invalidArg (nameof value) "A provider artifact set identity must be an uppercase SHA-256 digest."
        ProviderArtifactSetId value

    let value (ProviderArtifactSetId value) = value

type ProviderArtifactFile =
    { RelativePath: string
      Sha256: string }

type ProviderArtifactSet =
    { Identity: ProviderArtifactSetId
      SourceRoot: string
      Files: ProviderArtifactFile list
      ExecutablePath: string
      Arguments: string list }

[<RequireQualifiedAccess>]
module ProviderArtifactSetIdentity =
    let private appendInt (hash: IncrementalHash) value =
        let bytes = Array.zeroCreate<byte> sizeof<int>
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(), value)
        hash.AppendData bytes

    let private appendString hash (value: string) =
        let bytes = Encoding.UTF8.GetBytes value
        appendInt hash bytes.Length
        hash.AppendData bytes

    let compute (files: ProviderArtifactFile list) (executablePath: string) (arguments: string list) =
        use hash = IncrementalHash.CreateHash HashAlgorithmName.SHA256
        appendString hash "CBI32"
        let ordered = files |> List.sortBy _.RelativePath
        appendInt hash ordered.Length
        for file in ordered do
            appendString hash file.RelativePath
            appendString hash file.Sha256
        appendString hash executablePath
        appendInt hash arguments.Length
        arguments |> List.iter (appendString hash)
        hash.GetHashAndReset() |> Convert.ToHexString |> ProviderArtifactSetId.create

type ProviderArtifactSetFailure =
    { Code: string
      Reason: string }

type StagedProviderArtifactSet =
    { Identity: ProviderArtifactSetId
      RootPath: string
      Files: ProviderArtifactFile list
      ExecutablePath: string
      Arguments: string list
      Reused: bool }

[<RequireQualifiedAccess>]
type ProviderArtifactStagingResult =
    | Staged of StagedProviderArtifactSet
    | Refused of ProviderArtifactSetFailure

type ProviderArtifactRemoval =
    { Code: string
      Removed: bool }

type StagedProviderProcess internal (
    inner: LocalProviderProcess,
    stagedArtifacts: StagedProviderArtifactSet,
    release: unit -> unit) =
    let mutable disposed = false

    member _.Conversation = inner.Conversation
    member internal _.StagedArtifacts = stagedArtifacts
    member _.Isolation = inner.Isolation
    member _.HasExited = inner.HasExited
    member _.WaitForExit(timeout: TimeSpan) = inner.WaitForExit timeout

    member _.Dispose() =
        if not disposed then
            inner.Dispose()
            release ()
            disposed <- true

    interface IDisposable with
        member this.Dispose() = this.Dispose()

[<RequireQualifiedAccess>]
type StagedProviderActivation =
    | Launched of StagedProviderProcess
    | Refused of ProviderArtifactSetFailure

type ContentAddressedProviderStore(rootPath: string) =
    let syncRoot = obj ()
    let leases = Dictionary<ProviderArtifactSetId, int>()
    let storeRoot = Path.GetFullPath rootPath

    let digest path =
        use stream = File.OpenRead path
        SHA256.HashData stream |> Convert.ToHexString

    let isDigest (value: string) =
        not (String.IsNullOrEmpty value)
        && value.Length = 64
        && value |> Seq.forall (fun character ->
            (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))

    let safeRelativePath (path: string) =
        not (String.IsNullOrWhiteSpace path)
        && not (Path.IsPathFullyQualified path)
        && not (path.Contains '\\')
        && (path.Split '/' |> Array.forall (fun segment -> segment.Length > 0 && segment <> "." && segment <> ".."))

    let combineRelative (root: string) (relative: string) =
        Path.GetFullPath(Path.Combine(Path.GetFullPath root, relative.Replace('/', Path.DirectorySeparatorChar)))

    let expectedRoot identity = Path.Combine(storeRoot, ProviderArtifactSetId.value identity)

    let deleteTree path =
        let rec remove attempt =
            if Directory.Exists path then
                try
                    Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    |> Seq.iter (fun file -> File.SetAttributes(file, FileAttributes.Normal))
                    Directory.Delete(path, true)
                with
                | :? IOException when attempt < 4 ->
                    Threading.Thread.Sleep 25
                    remove (attempt + 1)
                | :? UnauthorizedAccessException when attempt < 4 ->
                    Threading.Thread.Sleep 25
                    remove (attempt + 1)
        remove 0

    let copyVerified source destination expectedDigest =
        let rec copy attempt =
            try
                File.Copy(source, destination, false)
                let matches = String.Equals(digest destination, expectedDigest, StringComparison.Ordinal)
                if matches then
                    File.SetAttributes(destination, File.GetAttributes(destination) ||| FileAttributes.ReadOnly)
                matches
            with
            | :? IOException when attempt < 4 ->
                if File.Exists destination then
                    File.SetAttributes(destination, FileAttributes.Normal)
                    File.Delete destination
                Threading.Thread.Sleep 25
                copy (attempt + 1)
            | :? UnauthorizedAccessException when attempt < 4 ->
                if File.Exists destination then
                    File.SetAttributes(destination, FileAttributes.Normal)
                    File.Delete destination
                Threading.Thread.Sleep 25
                copy (attempt + 1)
        copy 0

    let moveDirectory source destination =
        let rec move attempt =
            try
                Directory.Move(source, destination)
            with
            | :? IOException when attempt < 4 ->
                Threading.Thread.Sleep 25
                move (attempt + 1)
            | :? UnauthorizedAccessException when attempt < 4 ->
                Threading.Thread.Sleep 25
                move (attempt + 1)
        move 0

    let snapshot (declaration: ProviderArtifactSet) root reused =
        { Identity = declaration.Identity
          RootPath = root
          Files = declaration.Files |> List.map id
          ExecutablePath = declaration.ExecutablePath
          Arguments = declaration.Arguments |> List.map id
          Reused = reused }

    let refuse code reason =
        ProviderArtifactStagingResult.Refused { Code = code; Reason = reason }

    let validate (declaration: ProviderArtifactSet) =
        if List.isEmpty declaration.Files
           || (declaration.Arguments |> List.exists String.IsNullOrEmpty)
           || not (safeRelativePath declaration.ExecutablePath)
           || (declaration.Files |> List.exists (fun file ->
               not (safeRelativePath file.RelativePath) || not (isDigest file.Sha256)))
           || (declaration.Files |> List.map _.RelativePath |> List.distinct |> List.length) <> declaration.Files.Length
           || not (declaration.Files |> List.exists (fun file -> file.RelativePath = declaration.ExecutablePath))
           || ProviderArtifactSetIdentity.compute declaration.Files declaration.ExecutablePath declaration.Arguments
              <> declaration.Identity then
            Some(refuse "artifact-set-invalid" "The provider artifact set manifest is not canonical and complete.")
        else
            None

    let verifyExisting (declaration: ProviderArtifactSet) root reused =
        let comparison =
            if OperatingSystem.IsWindows() then StringComparison.OrdinalIgnoreCase else StringComparison.Ordinal
        if not (String.Equals(Path.GetFullPath root, expectedRoot declaration.Identity, comparison))
           || not (Directory.Exists root) then
            refuse
                "staged-artifact-integrity-failed"
                "The staged artifact set is absent or does not belong to its content identity."
        else
            try
                let actualPaths =
                    Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    |> Seq.map (fun path -> Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
                    |> Seq.sort
                    |> Seq.toList
                let declaredPaths = declaration.Files |> List.map _.RelativePath |> List.sort
                if actualPaths <> declaredPaths
                   || (declaration.Files |> List.exists (fun file ->
                       not (String.Equals(digest (combineRelative root file.RelativePath), file.Sha256, StringComparison.Ordinal)))) then
                    refuse
                        "staged-artifact-integrity-failed"
                        "The existing staged artifact set does not match its content identity."
                else
                    ProviderArtifactStagingResult.Staged(snapshot declaration root reused)
            with
            | :? IOException
            | :? UnauthorizedAccessException ->
                refuse
                    "staged-artifact-integrity-failed"
                    "The existing staged artifact set could not be verified."

    do Directory.CreateDirectory storeRoot |> ignore

    member _.Stage(declaration: ProviderArtifactSet) =
        lock syncRoot (fun () ->
            match validate declaration with
            | Some failure -> failure
            | None ->
                let finalRoot = expectedRoot declaration.Identity
                if Directory.Exists finalRoot then
                    verifyExisting declaration finalRoot true
                else
                    let temporaryRoot = Path.Combine(storeRoot, $".stage-{Guid.NewGuid():N}")
                    Directory.CreateDirectory temporaryRoot |> ignore
                    try
                        let mutable failure: ProviderArtifactStagingResult option = None
                        for file in declaration.Files |> List.sortBy _.RelativePath do
                            if failure.IsNone then
                                let source = combineRelative declaration.SourceRoot file.RelativePath
                                if not (File.Exists source) || File.GetAttributes(source).HasFlag FileAttributes.Directory then
                                    failure <-
                                        Some(
                                            refuse
                                                "artifact-set-unavailable"
                                                $"Artifact set member '{file.RelativePath}' is unavailable.")
                                else
                                    let destination = combineRelative temporaryRoot file.RelativePath
                                    match Path.GetDirectoryName destination |> Option.ofObj with
                                    | Some parent -> Directory.CreateDirectory parent |> ignore
                                    | None -> failwith "A staged member must have a parent directory."
                                    if not (copyVerified source destination file.Sha256) then
                                        failure <-
                                            Some(
                                                refuse
                                                    "artifact-set-integrity-failed"
                                                    $"Artifact set member '{file.RelativePath}' does not match its declared digest.")
                        match failure with
                        | Some value ->
                            deleteTree temporaryRoot
                            value
                        | None ->
                            moveDirectory temporaryRoot finalRoot
                            ProviderArtifactStagingResult.Staged(snapshot declaration finalRoot false)
                    with
                    | :? IOException
                    | :? UnauthorizedAccessException ->
                        deleteTree temporaryRoot
                        refuse
                            "artifact-set-stage-failed"
                            $"Artifact set '{ProviderArtifactSetId.value declaration.Identity}' could not be staged transactionally.")

    member _.Activate(staged: StagedProviderArtifactSet, allowedArguments: string list) =
        lock syncRoot (fun () ->
            let declaration: ProviderArtifactSet =
                { Identity = staged.Identity
                  SourceRoot = staged.RootPath
                  Files = staged.Files
                  ExecutablePath = staged.ExecutablePath
                  Arguments = staged.Arguments }
            match verifyExisting declaration (expectedRoot staged.Identity) staged.Reused with
            | ProviderArtifactStagingResult.Refused failure ->
                StagedProviderActivation.Refused failure
            | ProviderArtifactStagingResult.Staged verified ->
                let executable = staged.Files |> List.find (fun file -> file.RelativePath = staged.ExecutablePath)
                match
                    LocalProviderArtifactActivator.acquireAndLaunch
                        { Identity = ProviderArtifactSetId.value staged.Identity
                          SourcePath = combineRelative staged.RootPath staged.ExecutablePath
                          Sha256 = executable.Sha256
                          Arguments = staged.Arguments }
                        { AllowedRoot = staged.RootPath
                          AllowedArguments = allowedArguments }
                with
                | LocalProviderActivation.Refused failure ->
                    StagedProviderActivation.Refused { Code = failure.Code; Reason = failure.Reason }
                | LocalProviderActivation.Launched owner ->
                    let mutable count = 0
                    if leases.TryGetValue(staged.Identity, &count) then
                        leases[staged.Identity] <- count + 1
                    else
                        leases[staged.Identity] <- 1
                    let release () =
                        lock syncRoot (fun () ->
                            let mutable current = 0
                            if leases.TryGetValue(staged.Identity, &current) then
                                if current <= 1 then leases.Remove staged.Identity |> ignore
                                else leases[staged.Identity] <- current - 1)
                    StagedProviderActivation.Launched(new StagedProviderProcess(owner, verified, release)))

    member _.Remove(identity: ProviderArtifactSetId) =
        lock syncRoot (fun () ->
            let mutable count = 0
            if leases.TryGetValue(identity, &count) && count > 0 then
                { Code = "artifact-set-in-use"; Removed = false }
            else
                let path = expectedRoot identity
                if not (Directory.Exists path) then
                    { Code = "artifact-set-not-staged"; Removed = false }
                else
                    try
                        deleteTree path
                        { Code = "removed"; Removed = true }
                    with
                    | :? IOException
                    | :? UnauthorizedAccessException ->
                        { Code = "artifact-set-removal-failed"; Removed = false })
