namespace Brontide.Minimal.ComponentManagement.Tests

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement

[<TestFixture>]
type AuthorityComparisonTests() =
    let implementation = "minimal-fsharp"
    let multiple action = Assert.Multiple(Action action)

    let scenarios () =
        let path =
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "component-management",
                "fixtures",
                "cm6-authority-comparison-vectors.json"
            )
        File.ReadAllText path |> FakeAuthorityComparison.loadFixture

    let profile (response: string) =
        use document = JsonDocument.Parse response
        document.RootElement.GetProperty("profile").GetRawText()

    let errorCode (response: string) =
        use document = JsonDocument.Parse response
        document.RootElement.GetProperty("protocolError").GetProperty("code").GetString()
        |> Option.ofObj
        |> Option.defaultValue ""

    let hasProperty (element: JsonElement) (name: string) =
        let mutable value = JsonElement()
        element.TryGetProperty(name, &value)

    let startForeign (variable: string) (label: string) (arguments: string list) : Process =
        match Environment.GetEnvironmentVariable variable |> Option.ofObj with
        | Some path when File.Exists path ->
            let info = ProcessStartInfo(path, String.concat " " arguments)
            info.RedirectStandardInput <- true
            info.RedirectStandardOutput <- true
            info.RedirectStandardError <- true
            info.UseShellExecute <- false
            info.CreateNoWindow <- true
            match Process.Start info |> Option.ofObj with
            | Some provider -> provider
            | None -> failwithf "The %s provider process did not start." label
        | _ ->
            Assert.Ignore(sprintf "%s does not name a built %s provider endpoint." variable label)
            failwith "unreachable"

    [<Test>]
    member _.``neutral scenarios cover every CM5 outcome without implementation behavior``() =
        let values = scenarios ()
        multiple (fun () ->
            Assert.That(
                values |> List.map (fun item -> item.Id),
                Is.EqualTo<string list>
                    [ "cm6-01-admitted"
                      "cm6-02-no-mapping"
                      "cm6-03-partial"
                      "cm6-04-unlimited"
                      "cm6-05-revoked"
                      "cm6-06-expired"
                      "cm6-07-policy-mistake"
                      "cm6-08-invalid-request" ]
            )
            Assert.That(
                values |> List.map (fun item -> item.ExpectedOutcome) |> Set.ofList,
                Is.EqualTo<Set<string>>
                    (Set.ofList [ "admitted"; "partially-admitted"; "denied"; "invalid-request" ])
            )
            Assert.That(values |> List.forall (fun item -> not (item.Json.Contains "algorithm")), Is.True)
            Assert.That(values |> List.forall (fun item -> not (item.Json.Contains "Reference")), Is.True)
            Assert.That(values |> List.forall (fun item -> not (item.Json.Contains "Minimal")), Is.True))

    [<Test>]
    member _.``every native profile has the declared outcome and complete sections``() =
        scenarios ()
        |> List.iter (fun scenario ->
            use response = JsonDocument.Parse(FakeAuthorityComparison.evaluate scenario.Json implementation)
            let root = response.RootElement
            let result = root.GetProperty "profile"
            multiple (fun () ->
                Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo 1, scenario.Id)
                Assert.That(root.GetProperty("implementation").GetString(), Is.EqualTo implementation, scenario.Id)
                Assert.That(root.GetProperty("scenario").GetString(), Is.EqualTo scenario.Id, scenario.Id)
                Assert.That(result.GetProperty("outcome").GetString(), Is.EqualTo scenario.ExpectedOutcome, scenario.Id)
                Assert.That(hasProperty result "evidenceDecisions", Is.True, scenario.Id)
                Assert.That(hasProperty result "relationshipDecisions", Is.True, scenario.Id)
                Assert.That(hasProperty result "authorityDecisions", Is.True, scenario.Id)
                Assert.That(hasProperty result "relationships", Is.True, scenario.Id)
                Assert.That(hasProperty result "grants", Is.True, scenario.Id)
                Assert.That(hasProperty result "policyMistakes", Is.True, scenario.Id)
                Assert.That(hasProperty result "decisionLog", Is.True, scenario.Id)))

    [<Test>]
    member _.``JSON lines is stateless ordered and one response per input``() =
        task {
            let values = scenarios () |> List.take 3
            use input = new StringReader(values |> List.map (fun item -> item.Json) |> String.concat Environment.NewLine)
            use output = new StringWriter()
            do! FakeAuthorityComparison.run input output implementation Threading.CancellationToken.None
            let identities =
                output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (fun line ->
                    use document = JsonDocument.Parse line
                    document.RootElement.GetProperty("scenario").GetString())
                |> Array.toList
            Assert.That(
                identities,
                Is.EqualTo<string list>(values |> List.map (fun item -> item.Id))
            )
        }

    [<Test>]
    member _.``protocol errors remain separate from CM5 outcomes``() =
        let baseline = (scenarios ()).Head.Json
        let malformed = FakeAuthorityComparison.evaluate "{" implementation
        let unsupported =
            baseline.Replace("\"schemaVersion\":1", "\"schemaVersion\":2", StringComparison.Ordinal)
            |> fun value -> FakeAuthorityComparison.evaluate value implementation
        let token =
            baseline.Replace("\"kind\":\"attached-device\"", "\"kind\":\"invented\"", StringComparison.Ordinal)
            |> fun value -> FakeAuthorityComparison.evaluate value implementation
        multiple (fun () ->
            Assert.That(errorCode malformed, Is.EqualTo "malformed-json")
            Assert.That(errorCode unsupported, Is.EqualTo "unsupported-schema")
            Assert.That(errorCode token, Is.EqualTo "unknown-token")
            Assert.That(malformed, Does.Not.Contain "\"profile\"")
            Assert.That(unsupported, Does.Not.Contain "\"profile\"")
            Assert.That(token, Does.Not.Contain "\"profile\""))

    [<Test>]
    member _.``oversized lines fail at the protocol boundary``() =
        let response =
            FakeAuthorityComparison.evaluate (String(' ', 1_048_577)) implementation
        multiple (fun () ->
            Assert.That(errorCode response, Is.EqualTo "invalid-envelope")
            Assert.That(response, Does.Not.Contain "\"profile\""))

    [<Test>]
    member _.``repeated evaluation is byte stable``() =
        let scenario = (scenarios ()).Head
        let first = FakeAuthorityComparison.evaluate scenario.Json implementation
        let second = FakeAuthorityComparison.evaluate scenario.Json implementation
        Assert.That(second, Is.EqualTo first)

    [<Test>]
    member _.``semantic enumeration permutation preserves the complete profile``() =
        let scenario =
            scenarios () |> List.find (fun item -> item.Id = "cm6-03-partial")
        let root =
            JsonNode.Parse(scenario.Json)
            |> Option.ofObj
            |> Option.defaultWith (fun () -> failwith "Scenario JSON did not produce a node.")
            |> fun node -> node.AsObject()
        let reverse (array: JsonArray) =
            let values =
                array
                |> Seq.choose Option.ofObj
                |> Seq.map (fun item -> item.DeepClone())
                |> Seq.rev
                |> Seq.toList
            array.Clear()
            values |> List.iter array.Add
        root["authority"]
        |> Option.ofObj
        |> Option.defaultWith (fun () -> failwith "Scenario authority is missing.")
        |> fun node -> node.AsArray()
        |> reverse
        root["policy"]
        |> Option.ofObj
        |> Option.defaultWith (fun () -> failwith "Scenario policy is missing.")
        |> fun node -> node.AsObject()["authority"]
        |> Option.ofObj
        |> Option.defaultWith (fun () -> failwith "Policy authority is missing.")
        |> fun node -> node.AsArray()
        |> reverse
        Assert.That(
            profile (FakeAuthorityComparison.evaluate (root.ToJsonString()) implementation),
            Is.EqualTo<string>(profile (FakeAuthorityComparison.evaluate scenario.Json implementation))
        )

    [<Test>]
    [<Category("CrossProcess")>]
    [<Category("CrossStack")>]
    member _.``Minimal profiles equal the Reference process for every scenario``() =
        task {
            use provider =
                startForeign
                    "BRONTIDE_REFERENCE_PROVIDER"
                    "Reference"
                    [ "--component-management" ]
            for scenario in scenarios () do
                do! provider.StandardInput.WriteLineAsync scenario.Json
                do! provider.StandardInput.FlushAsync()
                let! line = provider.StandardOutput.ReadLineAsync()
                let responseLine =
                    match line |> Option.ofObj with
                    | Some value -> value
                    | None -> failwithf "Reference provider closed before scenario %s." scenario.Id
                use response = JsonDocument.Parse responseLine
                multiple (fun () ->
                    Assert.That(
                        response.RootElement.GetProperty("implementation").GetString(),
                        Is.EqualTo "reference-csharp",
                        scenario.Id
                    )
                    Assert.That(
                        response.RootElement.GetProperty("profile").GetRawText(),
                        Is.EqualTo<string>(profile (FakeAuthorityComparison.evaluate scenario.Json implementation)),
                        scenario.Id
                    ))
            provider.StandardInput.Close()
            do! provider.WaitForExitAsync()
            let! error = provider.StandardError.ReadToEndAsync()
            Assert.That(provider.ExitCode, Is.Zero, error)
        }
