namespace Brontide.Minimal.Host

open System
open System.IO

[<StructuralEquality; StructuralComparison>]
type ProviderArtifactSourceId = private ProviderArtifactSourceId of string

[<RequireQualifiedAccess>]
module ProviderArtifactSourceId =
    let create value =
        if String.IsNullOrWhiteSpace value then invalidArg (nameof value) "A provider artifact source identity is required."
        ProviderArtifactSourceId value

    let value (ProviderArtifactSourceId value) = value

type ProviderArtifactAcquisitionFile =
    { RelativePath: string
      Sha256: string
      Length: int64 }

type ProviderArtifactAcquisitionRequest =
    { ExpectedSource: ProviderArtifactSourceId
      Identity: ProviderArtifactSetId
      Files: ProviderArtifactAcquisitionFile list
      ExecutablePath: string
      Arguments: string list
      MaxTotalBytes: int64 }

type IProviderArtifactSource =
    abstract Identity: ProviderArtifactSourceId
    abstract OpenRead: relativePath: string -> Stream option

type ProviderArtifactAcquisitionResult =
    { SourceIdentity: ProviderArtifactSourceId
      TransportCode: string
      PublisherEvidenceCode: string
      AdmissionCode: string
      Staged: StagedProviderArtifactSet option }
    member this.IsStaged = this.Staged.IsSome

type ProviderArtifactAcquirer(store: ContentAddressedProviderStore, transactionRoot: string) =
    let syncRoot = obj ()
    let root = Path.GetFullPath transactionRoot

    let safeRelativePath (path: string) =
        not (String.IsNullOrWhiteSpace path)
        && not (Path.IsPathFullyQualified path)
        && not (path.Contains '\\')
        && (path.Split '/' |> Array.forall (fun segment -> segment.Length > 0 && segment <> "." && segment <> ".."))

    let isDigest (value: string) =
        not (String.IsNullOrEmpty value)
        && value.Length = 64
        && value |> Seq.forall (fun character ->
            (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))

    let isValid (request: ProviderArtifactAcquisitionRequest) =
        let structural =
            not (List.isEmpty request.Files)
            && request.MaxTotalBytes > 0L
            && not (request.Arguments |> List.exists String.IsNullOrEmpty)
            && safeRelativePath request.ExecutablePath
            && (request.Files |> List.forall (fun file ->
                safeRelativePath file.RelativePath && isDigest file.Sha256 && file.Length >= 0L))
            && (request.Files |> List.map _.RelativePath |> List.distinct |> List.length) = request.Files.Length
            && (request.Files |> List.exists (fun file -> file.RelativePath = request.ExecutablePath))
        if not structural then
            false
        else
            let withinLimit, _ =
                request.Files
                |> List.fold (fun (valid, total) file ->
                    if not valid || file.Length > request.MaxTotalBytes - total then false, total
                    else true, total + file.Length) (true, 0L)
            let files = request.Files |> List.map (fun file -> { RelativePath = file.RelativePath; Sha256 = file.Sha256 })
            withinLimit
            && ProviderArtifactSetIdentity.compute files request.ExecutablePath request.Arguments = request.Identity

    let refused source code =
        { SourceIdentity = source
          TransportCode = code
          PublisherEvidenceCode = "publisher-evidence-not-evaluated"
          AdmissionCode = "admission-not-attempted"
          Staged = None }

    let copyExact (input: Stream) (output: Stream) length =
        let buffer = Array.zeroCreate<byte> 81920
        let mutable remaining = length
        let mutable exact = true
        while exact && remaining > 0L do
            let read = input.Read(buffer, 0, min buffer.Length (int remaining))
            if read = 0 then exact <- false
            else
                output.Write(buffer, 0, read)
                remaining <- remaining - int64 read
        exact && input.ReadByte() = -1

    let transportException (error: exn) =
        error :? IOException
        || error :? UnauthorizedAccessException
        || error :? ObjectDisposedException
        || error :? NotSupportedException

    let combineRelative (basePath: string) (relative: string) =
        Path.GetFullPath(Path.Combine(basePath, relative.Replace('/', Path.DirectorySeparatorChar)))

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

    let acquireMember (source: IProviderArtifactSource) transaction (memberFile: ProviderArtifactAcquisitionFile) =
        try
            match source.OpenRead memberFile.RelativePath with
            | None -> Some "acquisition-member-unavailable"
            | Some stream ->
                use input = stream
                let destination = combineRelative transaction memberFile.RelativePath
                match Path.GetDirectoryName destination |> Option.ofObj with
                | Some parent -> Directory.CreateDirectory parent |> ignore
                | None -> failwith "An acquired member must have a parent directory."
                use output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                if copyExact input output memberFile.Length then None
                else Some "acquisition-length-mismatch"
        with error when transportException error ->
            Some "acquisition-transport-failed"

    do
        if isNull (box store) then nullArg (nameof store)
        if String.IsNullOrWhiteSpace transactionRoot then invalidArg (nameof transactionRoot) "A transaction root is required."
        Directory.CreateDirectory root |> ignore

    member _.Acquire(request: ProviderArtifactAcquisitionRequest, source: IProviderArtifactSource) =
        if isNull (box source) then nullArg (nameof source)
        lock syncRoot (fun () ->
            if not (isValid request) then
                refused request.ExpectedSource "acquisition-invalid"
            elif source.Identity <> request.ExpectedSource then
                refused request.ExpectedSource "acquisition-source-mismatch"
            else
                let transaction = Path.Combine(root, $".acquire-{Guid.NewGuid():N}")
                Directory.CreateDirectory transaction |> ignore
                try
                    let failure =
                        request.Files
                        |> List.sortBy _.RelativePath
                        |> List.tryPick (acquireMember source transaction)
                    match failure with
                    | Some code -> refused request.ExpectedSource code
                    | None ->
                        let declaration: ProviderArtifactSet =
                            { Identity = request.Identity
                              SourceRoot = transaction
                              Files = request.Files |> List.map (fun file ->
                                  { RelativePath = file.RelativePath; Sha256 = file.Sha256 })
                              ExecutablePath = request.ExecutablePath
                              Arguments = request.Arguments }
                        match store.Stage declaration with
                        | ProviderArtifactStagingResult.Staged staged ->
                            { SourceIdentity = request.ExpectedSource
                              TransportCode = "transport-completed"
                              PublisherEvidenceCode = "publisher-evidence-not-evaluated"
                              AdmissionCode = "staged"
                              Staged = Some staged }
                        | ProviderArtifactStagingResult.Refused failure ->
                            { SourceIdentity = request.ExpectedSource
                              TransportCode = "transport-completed"
                              PublisherEvidenceCode = "publisher-evidence-not-evaluated"
                              AdmissionCode = failure.Code
                              Staged = None }
                finally
                    deleteTree transaction)
