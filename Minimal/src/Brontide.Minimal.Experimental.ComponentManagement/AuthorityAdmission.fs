namespace Brontide.Minimal.Experimental.ComponentManagement

open System

type ActorRelationshipKind =
    | AttachedDevice
    | ExternalPeer
    | ComponentParticipant

type AdmissionEvidenceVerification =
    | Verified
    | Unverified

type AdmissionEvidenceState =
    | Current
    | Revoked

type AdmissionEvidenceDecisionKind =
    | Accepted
    | UnverifiedEvidence
    | UntrustedIssuer
    | NotYetValid
    | Expired
    | RevokedEvidence
    | SubjectMismatch

type PolicyDisposition =
    | Allow
    | Deny

[<RequireQualifiedAccess>]
type AuthorityAdmissionOutcomeKind =
    | Admitted
    | PartiallyAdmitted
    | Denied
    | InvalidRequest

type AdmissionEvidence =
    { Evidence: EvidenceId
      Issuer: IssuerId
      Subject: ActorId
      Verification: AdmissionEvidenceVerification
      ValidFrom: DateTimeOffset
      ExpiresAt: DateTimeOffset
      State: AdmissionEvidenceState }

type ActorRelationshipRequest =
    { Request: RelationshipRequestId
      ProposedActor: ActorId
      Kind: ActorRelationshipKind
      Evidence: EvidenceId list }

type AuthorityRequest =
    { Request: AuthorityRequestId
      Relationship: RelationshipRequestId
      Capability: CapabilityId
      Target: ActorId
      Operation: OperationId
      Scope: CapabilityScopeId
      Unlimited: bool }

type RelationshipPolicyRule =
    { Rule: PolicyRuleId
      ProposedActor: ActorId
      Kind: ActorRelationshipKind
      Disposition: PolicyDisposition
      LocalActor: LocalActorReferenceId option
      RequiredEvidence: EvidenceId list
      KnownMistake: bool
      Rationale: string }

type AuthorityPolicyRule =
    { Rule: PolicyRuleId
      RelationshipKind: ActorRelationshipKind
      Capability: CapabilityId
      Target: ActorId
      Operation: OperationId
      Scope: CapabilityScopeId
      Disposition: PolicyDisposition
      KnownMistake: bool
      Rationale: string }

type LocalAuthorityPolicy =
    { Policy: AuthorityPolicyId
      TrustedIssuers: IssuerId list
      RelationshipRules: RelationshipPolicyRule list
      AuthorityRules: AuthorityPolicyRule list }

type AuthorityAdmissionRequest =
    { Request: AdmissionRequestId
      Participant: ActorId
      EvaluationTime: DateTimeOffset
      Evidence: AdmissionEvidence list
      Relationships: ActorRelationshipRequest list
      Authority: AuthorityRequest list
      Policy: LocalAuthorityPolicy }

type AdmissionEvidenceDecision =
    { Relationship: RelationshipRequestId
      Evidence: EvidenceId
      Kind: AdmissionEvidenceDecisionKind
      Reason: string }

type ActorRelationshipDecision =
    { Request: RelationshipRequestId
      ProposedActor: ActorId
      Kind: ActorRelationshipKind
      Admitted: bool
      LocalActor: LocalActorReferenceId option
      Rule: PolicyRuleId option
      Reason: string }

type AuthorityPolicyDecision =
    { Request: AuthorityRequestId
      Relationship: RelationshipRequestId
      Admitted: bool
      Rule: PolicyRuleId option
      Reason: string }

type EstablishedActorRelationship =
    { Request: RelationshipRequestId
      ProposedActor: ActorId
      Kind: ActorRelationshipKind
      LocalActor: LocalActorReferenceId
      Policy: AuthorityPolicyId
      Rule: PolicyRuleId }

type LocalCapabilityGrant =
    { Grant: CapabilityGrantId
      Request: AuthorityRequestId
      Holder: LocalActorReferenceId
      Capability: CapabilityId
      Target: ActorId
      Operation: OperationId
      Scope: CapabilityScopeId
      Policy: AuthorityPolicyId
      Rule: PolicyRuleId }

type PolicyMistakeFinding =
    { Policy: AuthorityPolicyId
      Rule: PolicyRuleId
      Request: string
      Decision: PolicyDisposition
      Rationale: string }

type AuthorityAdmissionObservation =
    { Request: AdmissionRequestId
      Policy: AuthorityPolicyId
      EvaluationTime: DateTimeOffset
      EvidenceDecisions: AdmissionEvidenceDecision list
      RelationshipDecisions: ActorRelationshipDecision list
      AuthorityDecisions: AuthorityPolicyDecision list
      Relationships: EstablishedActorRelationship list
      Grants: LocalCapabilityGrant list
      PolicyMistakes: PolicyMistakeFinding list
      DecisionLog: string list }

type AuthorityAdmissionOutcome =
    { Kind: AuthorityAdmissionOutcomeKind
      Observation: AuthorityAdmissionObservation
      Failure: string option }

[<RequireQualifiedAccess>]
module FakeAuthorityAdmission =
    let private duplicate values =
        values
        |> Seq.countBy id
        |> Seq.filter (fun (_, count) -> count > 1)
        |> Seq.map fst
        |> Seq.sort
        |> Seq.tryHead

    let private validate (request: AuthorityAdmissionRequest) =
        let invalid = ResizeArray<string>()
        if request.EvaluationTime = DateTimeOffset() then
            invalid.Add "evaluation time must be supplied explicitly"

        duplicate (request.Evidence |> Seq.map (fun item -> EvidenceId.value item.Evidence))
        |> Option.iter (fun value -> invalid.Add(sprintf "duplicate evidence identity '%s'" value))
        duplicate (request.Relationships |> Seq.map (fun item -> RelationshipRequestId.value item.Request))
        |> Option.iter (fun value -> invalid.Add(sprintf "duplicate relationship request identity '%s'" value))
        duplicate (request.Authority |> Seq.map (fun item -> AuthorityRequestId.value item.Request))
        |> Option.iter (fun value -> invalid.Add(sprintf "duplicate authority request identity '%s'" value))
        duplicate (
            request.Policy.RelationshipRules
            |> Seq.map (fun item -> ActorId.value item.ProposedActor, item.Kind)
        )
        |> Option.iter (fun value -> invalid.Add(sprintf "duplicate relationship policy mapping '%A'" value))
        duplicate (
            request.Policy.AuthorityRules
            |> Seq.map (fun item ->
                item.RelationshipKind,
                CapabilityId.value item.Capability,
                ActorId.value item.Target,
                OperationId.value item.Operation,
                CapabilityScopeId.value item.Scope)
        )
        |> Option.iter (fun value -> invalid.Add(sprintf "duplicate authority policy mapping '%A'" value))
        duplicate (request.Policy.TrustedIssuers |> Seq.map IssuerId.value)
        |> Option.iter (fun value -> invalid.Add(sprintf "trusted issuer '%s' is repeated" value))
        duplicate (
            Seq.append
                (request.Policy.RelationshipRules |> Seq.map (fun item -> PolicyRuleId.value item.Rule))
                (request.Policy.AuthorityRules |> Seq.map (fun item -> PolicyRuleId.value item.Rule))
        )
        |> Option.iter (fun value -> invalid.Add(sprintf "policy rule identity '%s' is repeated" value))
        request.Policy.RelationshipRules
        |> List.iter (fun item ->
            if item.Disposition = Allow && Option.isNone item.LocalActor then
                invalid.Add(
                    sprintf
                        "admitting relationship rule '%s' does not assign a local Actor reference"
                        (PolicyRuleId.value item.Rule)
                )
            if item.Disposition = Deny && Option.isSome item.LocalActor then
                invalid.Add(
                    sprintf
                        "denying relationship rule '%s' must not assign a local Actor reference"
                        (PolicyRuleId.value item.Rule)
                )
            duplicate (item.RequiredEvidence |> Seq.map EvidenceId.value)
            |> Option.iter (fun value ->
                invalid.Add(
                    sprintf
                        "relationship rule '%s' repeats required evidence '%s'"
                        (PolicyRuleId.value item.Rule)
                        value
                )))

        let evidenceIds = request.Evidence |> Seq.map (fun item -> item.Evidence) |> Set.ofSeq
        request.Evidence
        |> List.iter (fun item ->
            if item.ValidFrom >= item.ExpiresAt then
                invalid.Add(
                    sprintf
                        "evidence '%s' has an empty or negative validity interval"
                        (EvidenceId.value item.Evidence)
                ))
        request.Relationships
        |> List.iter (fun item ->
            if item.ProposedActor <> request.Participant then
                invalid.Add(
                    sprintf
                        "relationship '%s' does not name the request participant"
                        (RelationshipRequestId.value item.Request)
                )
            duplicate (item.Evidence |> Seq.map EvidenceId.value)
            |> Option.iter (fun value ->
                invalid.Add(
                    sprintf
                        "relationship '%s' repeats evidence '%s'"
                        (RelationshipRequestId.value item.Request)
                        value
                ))
            item.Evidence
            |> List.tryFind (fun evidence -> not (Set.contains evidence evidenceIds))
            |> Option.iter (fun evidence ->
                invalid.Add(
                    sprintf
                        "relationship '%s' names unknown evidence '%s'"
                        (RelationshipRequestId.value item.Request)
                        (EvidenceId.value evidence)
                )))

        let referencedEvidence =
            request.Relationships
            |> Seq.collect (fun item -> item.Evidence)
            |> Set.ofSeq
        request.Evidence
        |> List.tryFind (fun item -> not (Set.contains item.Evidence referencedEvidence))
        |> Option.iter (fun item ->
            invalid.Add(
                sprintf
                    "evidence '%s' is not presented by any relationship request"
                    (EvidenceId.value item.Evidence)
            ))

        let relationshipIds = request.Relationships |> Seq.map (fun item -> item.Request) |> Set.ofSeq
        request.Authority
        |> List.tryFind (fun item -> not (Set.contains item.Relationship relationshipIds))
        |> Option.iter (fun item ->
            invalid.Add(
                sprintf
                    "authority request '%s' names unknown relationship '%s'"
                    (AuthorityRequestId.value item.Request)
                    (RelationshipRequestId.value item.Relationship)
            ))
        invalid |> Seq.tryHead

    let private evidenceDecision
        (evaluationTime: DateTimeOffset)
        (trustedIssuers: Set<IssuerId>)
        (relationship: ActorRelationshipRequest)
        (evidence: AdmissionEvidence)
        =
        let kind, reason =
            if evidence.Subject <> relationship.ProposedActor then
                SubjectMismatch, "evidence subject does not match the proposed Actor"
            elif evidence.Verification <> Verified then
                UnverifiedEvidence, "evidence verification did not succeed"
            elif not (Set.contains evidence.Issuer trustedIssuers) then
                UntrustedIssuer, "issuer is not trusted by the receiving policy"
            elif evidence.State = Revoked then
                RevokedEvidence, "evidence is revoked"
            elif evaluationTime < evidence.ValidFrom then
                NotYetValid, "evidence is not yet valid"
            elif evaluationTime >= evidence.ExpiresAt then
                Expired, "evidence has expired"
            else
                Accepted, "evidence accepted for local policy evaluation"
        { Relationship = relationship.Request
          Evidence = evidence.Evidence
          Kind = kind
          Reason = reason }

    let private evidenceToken kind =
        match kind with
        | Accepted -> "accepted"
        | UnverifiedEvidence -> "unverified"
        | UntrustedIssuer -> "untrusted-issuer"
        | NotYetValid -> "not-yet-valid"
        | Expired -> "expired"
        | RevokedEvidence -> "revoked"
        | SubjectMismatch -> "subject-mismatch"

    let private observation
        (request: AuthorityAdmissionRequest)
        (evidence: AdmissionEvidenceDecision list)
        (relationships: ActorRelationshipDecision list)
        (authority: AuthorityPolicyDecision list)
        (established: EstablishedActorRelationship list)
        (grants: LocalCapabilityGrant list)
        (mistakes: PolicyMistakeFinding list)
        (log: string list)
        : AuthorityAdmissionObservation =
        { Request = request.Request
          Policy = request.Policy.Policy
          EvaluationTime = request.EvaluationTime
          EvidenceDecisions = evidence
          RelationshipDecisions = relationships
          AuthorityDecisions = authority
          Relationships = established
          Grants = grants
          PolicyMistakes = mistakes
          DecisionLog = log }

    let evaluate (request: AuthorityAdmissionRequest) =
        match validate request with
        | Some reason ->
            { Kind = AuthorityAdmissionOutcomeKind.InvalidRequest
              Observation = observation request [] [] [] [] [] [] [ sprintf "invalid:%s" reason ]
              Failure = Some reason }
        | None ->
            let evidenceById = request.Evidence |> List.map (fun item -> item.Evidence, item) |> Map.ofList
            let trustedIssuers = request.Policy.TrustedIssuers |> Set.ofList
            let relationshipRules =
                request.Policy.RelationshipRules
                |> List.map (fun item -> (item.ProposedActor, item.Kind), item)
                |> Map.ofList
            let authorityRules =
                request.Policy.AuthorityRules
                |> List.map (fun item ->
                    (item.RelationshipKind, item.Capability, item.Target, item.Operation, item.Scope), item)
                |> Map.ofList
            let relationshipById =
                request.Relationships |> List.map (fun item -> item.Request, item) |> Map.ofList
            let evidenceDecisions = ResizeArray<AdmissionEvidenceDecision>()
            let relationshipDecisions = ResizeArray<ActorRelationshipDecision>()
            let established = ResizeArray<EstablishedActorRelationship>()
            let establishedByRequest =
                System.Collections.Generic.Dictionary<RelationshipRequestId, EstablishedActorRelationship>()
            let authorityDecisions = ResizeArray<AuthorityPolicyDecision>()
            let grants = ResizeArray<LocalCapabilityGrant>()
            let mistakes = ResizeArray<PolicyMistakeFinding>()
            let log = ResizeArray<string>()

            let recordMistake rule requestValue disposition knownMistake rationale =
                if knownMistake then
                    mistakes.Add
                        { Policy = request.Policy.Policy
                          Rule = rule
                          Request = requestValue
                          Decision = disposition
                          Rationale = rationale }

            request.Relationships
            |> List.sortBy (fun item -> RelationshipRequestId.value item.Request)
            |> List.iter (fun relationship ->
                let decisions =
                    relationship.Evidence
                    |> List.sortBy EvidenceId.value
                    |> List.map (fun evidence ->
                        evidenceDecision
                            request.EvaluationTime
                            trustedIssuers
                            relationship
                            evidenceById[evidence])
                decisions |> List.iter evidenceDecisions.Add
                decisions
                |> List.iter (fun decision ->
                    log.Add(
                        sprintf
                            "evidence:%s:%s:%s"
                            (RelationshipRequestId.value relationship.Request)
                            (EvidenceId.value decision.Evidence)
                            (evidenceToken decision.Kind)
                    ))

                match Map.tryFind (relationship.ProposedActor, relationship.Kind) relationshipRules with
                | None ->
                    relationshipDecisions.Add
                        { Request = relationship.Request
                          ProposedActor = relationship.ProposedActor
                          Kind = relationship.Kind
                          Admitted = false
                          LocalActor = None
                          Rule = None
                          Reason = "receiving policy has no relationship mapping" }
                    log.Add(sprintf "relationship:%s:denied:no-policy-mapping" (RelationshipRequestId.value relationship.Request))
                | Some rule ->
                    let evidenceAccepted =
                        rule.RequiredEvidence
                        |> List.forall (fun required ->
                            decisions
                            |> List.exists (fun item ->
                                item.Evidence = required
                                && item.Kind = AdmissionEvidenceDecisionKind.Accepted))
                    if not evidenceAccepted then
                        relationshipDecisions.Add
                            { Request = relationship.Request
                              ProposedActor = relationship.ProposedActor
                              Kind = relationship.Kind
                              Admitted = false
                              LocalActor = None
                              Rule = Some rule.Rule
                              Reason = "required evidence was not accepted" }
                        log.Add(sprintf "relationship:%s:denied:evidence" (RelationshipRequestId.value relationship.Request))
                    else
                        recordMistake
                            rule.Rule
                            (RelationshipRequestId.value relationship.Request)
                            rule.Disposition
                            rule.KnownMistake
                            rule.Rationale
                        match rule.Disposition with
                        | Deny ->
                            relationshipDecisions.Add
                                { Request = relationship.Request
                                  ProposedActor = relationship.ProposedActor
                                  Kind = relationship.Kind
                                  Admitted = false
                                  LocalActor = None
                                  Rule = Some rule.Rule
                                  Reason = rule.Rationale }
                            log.Add(
                                sprintf
                                    "relationship:%s:denied:%s"
                                    (RelationshipRequestId.value relationship.Request)
                                    (PolicyRuleId.value rule.Rule)
                            )
                        | Allow ->
                            let localActor = rule.LocalActor |> Option.get
                            let accepted =
                                { Request = relationship.Request
                                  ProposedActor = relationship.ProposedActor
                                  Kind = relationship.Kind
                                  LocalActor = localActor
                                  Policy = request.Policy.Policy
                                  Rule = rule.Rule }
                            established.Add accepted
                            establishedByRequest.Add(relationship.Request, accepted)
                            relationshipDecisions.Add
                                { Request = relationship.Request
                                  ProposedActor = relationship.ProposedActor
                                  Kind = relationship.Kind
                                  Admitted = true
                                  LocalActor = Some localActor
                                  Rule = Some rule.Rule
                                  Reason = rule.Rationale }
                            log.Add(
                                sprintf
                                    "relationship:%s:admitted:%s:%s"
                                    (RelationshipRequestId.value relationship.Request)
                                    (PolicyRuleId.value rule.Rule)
                                    (LocalActorReferenceId.value localActor)
                            ))

            request.Authority
            |> List.sortBy (fun item -> AuthorityRequestId.value item.Request)
            |> List.iter (fun authority ->
                match establishedByRequest.TryGetValue authority.Relationship with
                | false, _ ->
                    authorityDecisions.Add
                        { Request = authority.Request
                          Relationship = authority.Relationship
                          Admitted = false
                          Rule = None
                          Reason = "dependent Actor relationship was not admitted" }
                    log.Add(sprintf "authority:%s:denied:relationship" (AuthorityRequestId.value authority.Request))
                | true, relationship when authority.Unlimited ->
                    authorityDecisions.Add
                        { Request = authority.Request
                          Relationship = authority.Relationship
                          Admitted = false
                          Rule = None
                          Reason = "unlimited authority is not a locally recognisable narrow grant" }
                    log.Add(sprintf "authority:%s:denied:unlimited" (AuthorityRequestId.value authority.Request))
                | true, relationship ->
                    let relationshipRequest = relationshipById[authority.Relationship]
                    let key =
                        relationshipRequest.Kind,
                        authority.Capability,
                        authority.Target,
                        authority.Operation,
                        authority.Scope
                    match Map.tryFind key authorityRules with
                    | None ->
                        authorityDecisions.Add
                            { Request = authority.Request
                              Relationship = authority.Relationship
                              Admitted = false
                              Rule = None
                              Reason = "receiving policy has no exact authority mapping" }
                        log.Add(sprintf "authority:%s:denied:no-policy-mapping" (AuthorityRequestId.value authority.Request))
                    | Some rule ->
                        recordMistake
                            rule.Rule
                            (AuthorityRequestId.value authority.Request)
                            rule.Disposition
                            rule.KnownMistake
                            rule.Rationale
                        match rule.Disposition with
                        | Deny ->
                            authorityDecisions.Add
                                { Request = authority.Request
                                  Relationship = authority.Relationship
                                  Admitted = false
                                  Rule = Some rule.Rule
                                  Reason = rule.Rationale }
                            log.Add(
                                sprintf
                                    "authority:%s:denied:%s"
                                    (AuthorityRequestId.value authority.Request)
                                    (PolicyRuleId.value rule.Rule)
                            )
                        | Allow ->
                            authorityDecisions.Add
                                { Request = authority.Request
                                  Relationship = authority.Relationship
                                  Admitted = true
                                  Rule = Some rule.Rule
                                  Reason = rule.Rationale }
                            grants.Add
                                { Grant =
                                    CapabilityGrantId.create(
                                        sprintf "grant.%s" (AuthorityRequestId.value authority.Request)
                                    )
                                  Request = authority.Request
                                  Holder = relationship.LocalActor
                                  Capability = authority.Capability
                                  Target = authority.Target
                                  Operation = authority.Operation
                                  Scope = authority.Scope
                                  Policy = request.Policy.Policy
                                  Rule = rule.Rule }
                            log.Add(
                                sprintf
                                    "authority:%s:admitted:%s"
                                    (AuthorityRequestId.value authority.Request)
                                    (PolicyRuleId.value rule.Rule)
                            ))

            let denied =
                (relationshipDecisions |> Seq.filter (fun item -> not item.Admitted) |> Seq.length)
                + (authorityDecisions |> Seq.filter (fun item -> not item.Admitted) |> Seq.length)
            let admitted =
                (relationshipDecisions |> Seq.filter (fun item -> item.Admitted) |> Seq.length)
                + (authorityDecisions |> Seq.filter (fun item -> item.Admitted) |> Seq.length)
            let kind =
                match admitted, denied with
                | value, 0 when value > 0 -> AuthorityAdmissionOutcomeKind.Admitted
                | value, deniedValue when value > 0 && deniedValue > 0 ->
                    AuthorityAdmissionOutcomeKind.PartiallyAdmitted
                | _ -> AuthorityAdmissionOutcomeKind.Denied
            { Kind = kind
              Observation =
                observation
                    request
                    (List.ofSeq evidenceDecisions)
                    (List.ofSeq relationshipDecisions)
                    (List.ofSeq authorityDecisions)
                    (List.ofSeq established)
                    (List.ofSeq grants)
                    (List.ofSeq mistakes)
                    (List.ofSeq log)
              Failure = None }
