namespace Brontide.Minimal.Host

open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open Brontide.Minimal.Binding.Portable

type LocalProviderArtifact =
    { Identity: string
      SourcePath: string
      Sha256: string
      Arguments: string list }

type LocalProviderLaunchPolicy =
    { AllowedRoot: string
      AllowedArguments: string list }

type LocalProviderArtifactFailure =
    { Code: string
      Reason: string }

type LocalProviderProcess internal (providerProcess: Process, artifact: LocalProviderArtifact, digest: string) =
    let conversation =
        PortableProcessConversation(
            PortableStreamDuplex(
                providerProcess.StandardOutput.BaseStream,
                providerProcess.StandardInput.BaseStream,
                PortableLimits.declared,
                false),
            PortableLimits.declared)
        :> IPortableProviderConversation
    let mutable disposed = false
    let mutable exited = false

    member _.Artifact = artifact
    member _.VerifiedSha256 = digest
    member _.Isolation = "dedicated-process"
    member _.UsesShell = providerProcess.StartInfo.UseShellExecute
    member _.RedirectsStandardStreams =
        providerProcess.StartInfo.RedirectStandardInput
        && providerProcess.StartInfo.RedirectStandardOutput
        && providerProcess.StartInfo.RedirectStandardError
    member _.Conversation = conversation
    member _.HasExited = exited || (not disposed && providerProcess.HasExited)

    member _.WaitForExit(timeout: TimeSpan) =
        providerProcess.HasExited || providerProcess.WaitForExit(int timeout.TotalMilliseconds)

    member _.Dispose() =
        if not disposed then
            conversation.Close()
            if not providerProcess.HasExited then
                providerProcess.Kill true
            providerProcess.WaitForExit()
            exited <- true
            providerProcess.Dispose()
            disposed <- true

    interface IDisposable with
        member this.Dispose() = this.Dispose()

[<RequireQualifiedAccess>]
type LocalProviderActivation =
    | Launched of LocalProviderProcess
    | Refused of LocalProviderArtifactFailure

[<RequireQualifiedAccess>]
module LocalProviderArtifactActivator =
    let private refuse code reason =
        LocalProviderActivation.Refused { Code = code; Reason = reason }

    let private containedBy allowedRoot sourcePath =
        let relative = Path.GetRelativePath(allowedRoot, sourcePath)
        not (Path.IsPathRooted relative)
        && relative <> ".."
        && not (relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        && not (relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))

    let private launch (environment: Map<string, string>) (artifact: LocalProviderArtifact) (policy: LocalProviderLaunchPolicy) =
        let sourcePath = Path.GetFullPath artifact.SourcePath
        if not (File.Exists sourcePath) || File.GetAttributes(sourcePath).HasFlag FileAttributes.Directory then
            refuse
                "artifact-unavailable"
                $"Artifact '{artifact.Identity}' is not an available regular file."
        else
            let digest =
                try
                    use stream = File.OpenRead sourcePath
                    Ok(SHA256.HashData stream |> Convert.ToHexString)
                with
                | :? IOException
                | :? UnauthorizedAccessException -> Error()
            match digest with
            | Error() ->
                refuse "artifact-unavailable" $"Artifact '{artifact.Identity}' could not be read."
            | Ok actualDigest when not (String.Equals(actualDigest, artifact.Sha256, StringComparison.Ordinal)) ->
                refuse
                    "artifact-integrity-failed"
                    $"Artifact '{artifact.Identity}' does not match its expected SHA-256 digest."
            | Ok actualDigest ->
                let allowedRoot = Path.GetFullPath policy.AllowedRoot
                if not (containedBy allowedRoot sourcePath)
                   || (artifact.Arguments |> List.exists String.IsNullOrEmpty)
                   || (policy.AllowedArguments |> List.exists String.IsNullOrEmpty)
                   || artifact.Arguments <> policy.AllowedArguments then
                    refuse "launch-policy-refused" $"Launch policy refused artifact '{artifact.Identity}'."
                else
                    let info = ProcessStartInfo()
                    info.FileName <- sourcePath
                    info.WorkingDirectory <- allowedRoot
                    info.RedirectStandardInput <- true
                    info.RedirectStandardOutput <- true
                    info.RedirectStandardError <- true
                    info.UseShellExecute <- false
                    info.CreateNoWindow <- true
                    artifact.Arguments |> List.iter info.ArgumentList.Add
                    environment |> Map.iter (fun key value -> info.Environment[key] <- value)
                    let providerProcess = new Process(StartInfo = info)
                    try
                        if providerProcess.Start() then
                            LocalProviderActivation.Launched(
                                new LocalProviderProcess(providerProcess, artifact, actualDigest))
                        else
                            providerProcess.Dispose()
                            refuse
                                "provider-process-start-failed"
                                $"Artifact '{artifact.Identity}' did not start."
                    with
                    | :? Win32Exception
                    | :? InvalidOperationException ->
                        providerProcess.Dispose()
                        refuse
                            "provider-process-start-failed"
                            $"Artifact '{artifact.Identity}' could not be started."

    let acquireAndLaunch artifact policy = launch Map.empty artifact policy

    let internal acquireAndLaunchWithEnvironment environment artifact policy =
        launch environment artifact policy
