namespace Brontide.Minimal.Experimental.ComponentManagement

open System
open System.Globalization
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks

type AuthorityComparisonScenario =
    { Id: string
      ExpectedOutcome: string
      Json: string }

type AuthorityComparisonProtocolException(code: string, message: string) =
    inherit Exception(message)
    member _.Code = code

/// CM6 JSON-lines process seam. Primitives become Minimal-native identifiers before CM5 executes,
/// while the canonical profile deliberately excludes the provider implementation identity.
[<RequireQualifiedAccess>]
module FakeAuthorityComparison =
    let private schemaVersion = 1
    let private maximumLineCharacters = 1_048_576

    let private invalid message =
        AuthorityComparisonProtocolException("invalid-envelope", message)

    let private unknown message =
        AuthorityComparisonProtocolException("unknown-token", message)

    let private parse (json: string) =
        try
            JsonDocument.Parse json
        with :? JsonException as error ->
            raise (AuthorityComparisonProtocolException("malformed-json", error.Message))

    let private requireObject (element: JsonElement) path =
        if element.ValueKind <> JsonValueKind.Object then
            raise (invalid (sprintf "%s: expected object" path))

    let private requireProperties (element: JsonElement) path (expected: string list) =
        let actual = element.EnumerateObject() |> Seq.map (fun property -> property.Name) |> Seq.toList
        actual
        |> Seq.countBy id
        |> Seq.tryFind (fun (_, count) -> count > 1)
        |> Option.iter (fun (name, _) -> raise (invalid (sprintf "%s: duplicate property '%s'" path name)))
        let expectedSet = Set.ofList expected
        actual
        |> List.tryFind (fun name -> not (Set.contains name expectedSet))
        |> Option.iter (fun name -> raise (invalid (sprintf "%s: unknown property '%s'" path name)))
        expected
        |> List.tryFind (fun name -> not (List.contains name actual))
        |> Option.iter (fun name -> raise (invalid (sprintf "%s: missing property '%s'" path name)))

    let private property (element: JsonElement) (name: string) (path: string) =
        match element.TryGetProperty name with
        | true, value -> value
        | false, _ -> raise (invalid (sprintf "%s: missing property '%s'" path name))

    let private scalarText (element: JsonElement) (path: string) =
        if element.ValueKind <> JsonValueKind.String then
            raise (invalid (sprintf "%s: expected string" path))
        match element.GetString() with
        | null -> raise (invalid (sprintf "%s: expected string" path))
        | value -> value

    let private text (element: JsonElement) (name: string) (path: string) =
        scalarText (property element name path) (sprintf "%s.%s" path name)

    let private boolean (element: JsonElement) (name: string) (path: string) =
        match (property element name path).ValueKind with
        | JsonValueKind.True -> true
        | JsonValueKind.False -> false
        | _ -> raise (invalid (sprintf "%s.%s: expected boolean" path name))

    let private elements (element: JsonElement) (name: string) (path: string) =
        let value = property element name path
        if value.ValueKind <> JsonValueKind.Array then
            raise (invalid (sprintf "%s.%s: expected array" path name))
        value.EnumerateArray() |> Seq.toList

    let private requireSchema (element: JsonElement) (path: string) =
        let value = property element "schemaVersion" path
        let mutable parsed = 0
        if value.ValueKind <> JsonValueKind.Number || not (value.TryGetInt32(&parsed)) then
            raise (invalid (sprintf "%s.schemaVersion: expected integer" path))
        if parsed <> schemaVersion then
            raise (
                AuthorityComparisonProtocolException(
                    "unsupported-schema",
                    sprintf "schema version %d is not supported" parsed
                )
            )

    let private timestamp element name path =
        let value = text element name path
        let mutable parsed = DateTimeOffset()
        if
            not (
                DateTimeOffset.TryParseExact(
                    value,
                    "yyyy-MM-dd'T'HH:mm:ssK",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    &parsed
                )
            )
        then
            raise (invalid (sprintf "%s.%s: expected an RFC 3339 whole-second timestamp" path name))
        parsed

    let private token element name path choices =
        let value = text element name path
        choices
        |> List.tryFind (fun (candidate, _) -> candidate = value)
        |> Option.map snd
        |> Option.defaultWith (fun () -> raise (unknown (sprintf "%s.%s: unknown token '%s'" path name value)))

    let private relationshipKind element name path =
        token
            element
            name
            path
            [ "attached-device", AttachedDevice
              "external-peer", ExternalPeer
              "component-participant", ComponentParticipant ]

    let private disposition element name path =
        token element name path [ "allow", Allow; "deny", Deny ]

    let private readEvidence (element: JsonElement) path : AdmissionEvidence =
        requireObject element path
        requireProperties
            element
            path
            [ "id"; "issuer"; "subject"; "verification"; "validFrom"; "expiresAt"; "state" ]
        { Evidence = EvidenceId.create (text element "id" path)
          Issuer = IssuerId.create (text element "issuer" path)
          Subject = ActorId.create (text element "subject" path)
          Verification =
            token
                element
                "verification"
                path
                [ "verified", Verified; "unverified", Unverified ]
          ValidFrom = timestamp element "validFrom" path
          ExpiresAt = timestamp element "expiresAt" path
          State = token element "state" path [ "current", Current; "revoked", Revoked ] }

    let private readRelationship (element: JsonElement) path : ActorRelationshipRequest =
        requireObject element path
        requireProperties element path [ "id"; "actor"; "kind"; "evidence" ]
        { Request = RelationshipRequestId.create (text element "id" path)
          ProposedActor = ActorId.create (text element "actor" path)
          Kind = relationshipKind element "kind" path
          Evidence =
            elements element "evidence" path
            |> List.mapi (fun index value ->
                EvidenceId.create (scalarText value (sprintf "%s.evidence[%d]" path index))) }

    let private readAuthority (element: JsonElement) path : AuthorityRequest =
        requireObject element path
        requireProperties
            element
            path
            [ "id"; "relationship"; "capability"; "target"; "operation"; "scope"; "unlimited" ]
        { Request = AuthorityRequestId.create (text element "id" path)
          Relationship = RelationshipRequestId.create (text element "relationship" path)
          Capability = CapabilityId.create (text element "capability" path)
          Target = ActorId.create (text element "target" path)
          Operation = OperationId.create (text element "operation" path)
          Scope = CapabilityScopeId.create (text element "scope" path)
          Unlimited = boolean element "unlimited" path }

    let private readRelationshipRule (element: JsonElement) path : RelationshipPolicyRule =
        requireObject element path
        requireProperties
            element
            path
            [ "id"
              "actor"
              "kind"
              "disposition"
              "localActor"
              "requiredEvidence"
              "knownMistake"
              "rationale" ]
        let localActor = property element "localActor" path
        let localActorValue =
            match localActor.ValueKind with
            | JsonValueKind.Null -> None
            | JsonValueKind.String ->
                Some(LocalActorReferenceId.create (scalarText localActor (sprintf "%s.localActor" path)))
            | _ -> raise (invalid (sprintf "%s.localActor: expected string or null" path))
        { Rule = PolicyRuleId.create (text element "id" path)
          ProposedActor = ActorId.create (text element "actor" path)
          Kind = relationshipKind element "kind" path
          Disposition = disposition element "disposition" path
          LocalActor = localActorValue
          RequiredEvidence =
            elements element "requiredEvidence" path
            |> List.mapi (fun index value ->
                EvidenceId.create (scalarText value (sprintf "%s.requiredEvidence[%d]" path index)))
          KnownMistake = boolean element "knownMistake" path
          Rationale = text element "rationale" path }

    let private readAuthorityRule (element: JsonElement) path : AuthorityPolicyRule =
        requireObject element path
        requireProperties
            element
            path
            [ "id"
              "relationshipKind"
              "capability"
              "target"
              "operation"
              "scope"
              "disposition"
              "knownMistake"
              "rationale" ]
        { Rule = PolicyRuleId.create (text element "id" path)
          RelationshipKind = relationshipKind element "relationshipKind" path
          Capability = CapabilityId.create (text element "capability" path)
          Target = ActorId.create (text element "target" path)
          Operation = OperationId.create (text element "operation" path)
          Scope = CapabilityScopeId.create (text element "scope" path)
          Disposition = disposition element "disposition" path
          KnownMistake = boolean element "knownMistake" path
          Rationale = text element "rationale" path }

    let private readPolicy (element: JsonElement) path : LocalAuthorityPolicy =
        requireObject element path
        requireProperties element path [ "id"; "trustedIssuers"; "relationships"; "authority" ]
        { Policy = AuthorityPolicyId.create (text element "id" path)
          TrustedIssuers =
            elements element "trustedIssuers" path
            |> List.mapi (fun index value ->
                IssuerId.create (scalarText value (sprintf "%s.trustedIssuers[%d]" path index)))
          RelationshipRules =
            elements element "relationships" path
            |> List.mapi (fun index value ->
                readRelationshipRule value (sprintf "%s.relationships[%d]" path index))
          AuthorityRules =
            elements element "authority" path
            |> List.mapi (fun index value ->
                readAuthorityRule value (sprintf "%s.authority[%d]" path index)) }

    let private readScenario (element: JsonElement) =
        requireObject element "scenario"
        requireProperties
            element
            "scenario"
            [ "schemaVersion"
              "id"
              "expectedOutcome"
              "evaluationTime"
              "participant"
              "evidence"
              "relationships"
              "authority"
              "policy" ]
        requireSchema element "scenario"
        let identity = text element "id" "scenario"
        let expected = text element "expectedOutcome" "scenario"
        if
            not (
                List.contains
                    expected
                    [ "admitted"; "partially-admitted"; "denied"; "invalid-request" ]
            )
        then
            raise (unknown (sprintf "scenario.expectedOutcome: unknown token '%s'" expected))
        let request =
            { Request = AdmissionRequestId.create (sprintf "admission.%s" identity)
              Participant = ActorId.create (text element "participant" "scenario")
              EvaluationTime = timestamp element "evaluationTime" "scenario"
              Evidence =
                elements element "evidence" "scenario"
                |> List.mapi (fun index value -> readEvidence value (sprintf "scenario.evidence[%d]" index))
              Relationships =
                elements element "relationships" "scenario"
                |> List.mapi (fun index value ->
                    readRelationship value (sprintf "scenario.relationships[%d]" index))
              Authority =
                elements element "authority" "scenario"
                |> List.mapi (fun index value ->
                    readAuthority value (sprintf "scenario.authority[%d]" index))
              Policy = readPolicy (property element "policy" "scenario") "scenario.policy" }
        identity, request

    let loadFixture (json: string) =
        use document = parse json
        let root = document.RootElement
        requireObject root "fixture"
        requireProperties root "fixture" [ "schemaVersion"; "fixture"; "scenarios" ]
        requireSchema root "fixture"
        if text root "fixture" "fixture" <> "cm6-authority-comparison-vectors" then
            raise (invalid "fixture: unknown fixture name")
        let scenarios =
            elements root "scenarios" "fixture"
            |> List.mapi (fun index scenario ->
                let path = sprintf "fixture.scenarios[%d]" index
                requireObject scenario path
                let identity = text scenario "id" path
                let expected = text scenario "expectedOutcome" path
                if
                    not (
                        List.contains
                            expected
                            [ "admitted"; "partially-admitted"; "denied"; "invalid-request" ]
                    )
                then
                    raise (unknown (sprintf "%s.expectedOutcome: unknown token '%s'" path expected))
                { Id = identity
                  ExpectedOutcome = expected
                  Json = JsonSerializer.Serialize scenario })
        scenarios
        |> Seq.countBy (fun item -> item.Id)
        |> Seq.tryFind (fun (_, count) -> count > 1)
        |> Option.iter (fun (identity, _) ->
            raise (invalid (sprintf "fixture: duplicate scenario identity '%s'" identity)))
        scenarios

    let private stringNode (value: string) : JsonNode | null = JsonValue.Create(value)
    let private boolNode (value: bool) : JsonNode | null = JsonValue.Create(value)
    let private intNode (value: int) : JsonNode | null = JsonValue.Create(value)

    let private setString (node: JsonObject) (name: string) (value: string) =
        node[name] <- stringNode value

    let private setBoolean (node: JsonObject) (name: string) (value: bool) =
        node[name] <- boolNode value

    let private setInteger (node: JsonObject) (name: string) (value: int) =
        node[name] <- intNode value

    let private setOptionalString (node: JsonObject) (name: string) (value: string option) =
        match value with
        | Some text -> setString node name text
        | None -> node[name] <- null

    let private arrayOf values project =
        let array = JsonArray()
        values |> Seq.iter (project >> array.Add)
        array

    let private relationshipToken value =
        match value with
        | AttachedDevice -> "attached-device"
        | ExternalPeer -> "external-peer"
        | ComponentParticipant -> "component-participant"

    let private evidenceDecisionToken value =
        match value with
        | AdmissionEvidenceDecisionKind.Accepted -> "accepted"
        | UnverifiedEvidence -> "unverified"
        | UntrustedIssuer -> "untrusted-issuer"
        | NotYetValid -> "not-yet-valid"
        | Expired -> "expired"
        | RevokedEvidence -> "revoked"
        | SubjectMismatch -> "subject-mismatch"

    let private dispositionToken value =
        match value with
        | Allow -> "allow"
        | Deny -> "deny"

    let private outcomeToken value =
        match value with
        | AuthorityAdmissionOutcomeKind.Admitted -> "admitted"
        | AuthorityAdmissionOutcomeKind.PartiallyAdmitted -> "partially-admitted"
        | AuthorityAdmissionOutcomeKind.Denied -> "denied"
        | AuthorityAdmissionOutcomeKind.InvalidRequest -> "invalid-request"

    let private time (value: DateTimeOffset) =
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)

    let private writeProfile (outcome: AuthorityAdmissionOutcome) =
        let observation = outcome.Observation
        let profile = JsonObject()
        setString profile "outcome" (outcomeToken outcome.Kind)
        setOptionalString profile "failure" outcome.Failure
        setString profile "request" (AdmissionRequestId.value observation.Request)
        setString profile "policy" (AuthorityPolicyId.value observation.Policy)
        setString profile "evaluationTime" (time observation.EvaluationTime)
        profile["evidenceDecisions"] <-
            arrayOf observation.EvidenceDecisions (fun item ->
                let node = JsonObject()
                setString node "relationship" (RelationshipRequestId.value item.Relationship)
                setString node "evidence" (EvidenceId.value item.Evidence)
                setString node "kind" (evidenceDecisionToken item.Kind)
                setString node "reason" item.Reason
                node :> JsonNode)
        profile["relationshipDecisions"] <-
            arrayOf observation.RelationshipDecisions (fun item ->
                let node = JsonObject()
                setString node "request" (RelationshipRequestId.value item.Request)
                setString node "actor" (ActorId.value item.ProposedActor)
                setString node "kind" (relationshipToken item.Kind)
                setBoolean node "admitted" item.Admitted
                setOptionalString node "localActor" (item.LocalActor |> Option.map LocalActorReferenceId.value)
                setOptionalString node "rule" (item.Rule |> Option.map PolicyRuleId.value)
                setString node "reason" item.Reason
                node :> JsonNode)
        profile["authorityDecisions"] <-
            arrayOf observation.AuthorityDecisions (fun item ->
                let node = JsonObject()
                setString node "request" (AuthorityRequestId.value item.Request)
                setString node "relationship" (RelationshipRequestId.value item.Relationship)
                setBoolean node "admitted" item.Admitted
                setOptionalString node "rule" (item.Rule |> Option.map PolicyRuleId.value)
                setString node "reason" item.Reason
                node :> JsonNode)
        profile["relationships"] <-
            arrayOf observation.Relationships (fun item ->
                let node = JsonObject()
                setString node "request" (RelationshipRequestId.value item.Request)
                setString node "actor" (ActorId.value item.ProposedActor)
                setString node "kind" (relationshipToken item.Kind)
                setString node "localActor" (LocalActorReferenceId.value item.LocalActor)
                setString node "policy" (AuthorityPolicyId.value item.Policy)
                setString node "rule" (PolicyRuleId.value item.Rule)
                node :> JsonNode)
        profile["grants"] <-
            arrayOf observation.Grants (fun item ->
                let node = JsonObject()
                setString node "grant" (CapabilityGrantId.value item.Grant)
                setString node "request" (AuthorityRequestId.value item.Request)
                setString node "holder" (LocalActorReferenceId.value item.Holder)
                setString node "capability" (CapabilityId.value item.Capability)
                setString node "target" (ActorId.value item.Target)
                setString node "operation" (OperationId.value item.Operation)
                setString node "scope" (CapabilityScopeId.value item.Scope)
                setString node "policy" (AuthorityPolicyId.value item.Policy)
                setString node "rule" (PolicyRuleId.value item.Rule)
                node :> JsonNode)
        profile["policyMistakes"] <-
            arrayOf observation.PolicyMistakes (fun item ->
                let node = JsonObject()
                setString node "policy" (AuthorityPolicyId.value item.Policy)
                setString node "rule" (PolicyRuleId.value item.Rule)
                setString node "request" item.Request
                setString node "decision" (dispositionToken item.Decision)
                setString node "rationale" item.Rationale
                node :> JsonNode)
        profile["decisionLog"] <-
            arrayOf observation.DecisionLog stringNode
        profile

    let canonicalProfile (outcome: AuthorityAdmissionOutcome) =
        (writeProfile outcome).ToJsonString()

    let private profileResponse implementation scenario outcome =
        let response = JsonObject()
        setInteger response "schemaVersion" schemaVersion
        setString response "implementation" implementation
        setString response "scenario" scenario
        response["profile"] <- writeProfile outcome
        response.ToJsonString()

    let private protocolError implementation code detail =
        let response = JsonObject()
        setInteger response "schemaVersion" schemaVersion
        setString response "implementation" implementation
        let error = JsonObject()
        setString error "code" code
        setString error "detail" detail
        response["protocolError"] <- error
        response.ToJsonString()

    let evaluate (scenarioJson: string) implementation =
        if String.IsNullOrWhiteSpace implementation then
            invalidArg "implementation" "implementation identity is required"
        if scenarioJson.Length > maximumLineCharacters then
            protocolError
                implementation
                "invalid-envelope"
                (sprintf "input line exceeds %d characters" maximumLineCharacters)
        else
            try
                use document = parse scenarioJson
                let identity, request = readScenario document.RootElement
                FakeAuthorityAdmission.evaluate request
                |> profileResponse implementation identity
            with
            | :? AuthorityComparisonProtocolException as error ->
                protocolError implementation error.Code error.Message
            | :? ArgumentException as error ->
                protocolError implementation "invalid-envelope" error.Message

    let run
        (input: TextReader)
        (output: TextWriter)
        implementation
        (cancellationToken: CancellationToken)
        : Task =
        task {
            let mutable reading = true
            while reading do
                let! line = input.ReadLineAsync(cancellationToken)
                match line with
                | null -> reading <- false
                | value ->
                    let response =
                        if String.IsNullOrWhiteSpace value then
                            protocolError
                                implementation
                                "invalid-envelope"
                                "input line must contain one JSON object"
                        else
                            evaluate value implementation
                    do! output.WriteLineAsync(response)
                    do! output.FlushAsync(cancellationToken)
        }
