namespace Brontide.Minimal.Binding.Portable

/// One immutable, inspectable Binding Plan for one binding scope.
///
/// Every fact is fixed when negotiation succeeds and never changes. A change requires a new binding
/// scope with a new plan identifier: there is no renegotiation in place. Each fact is also
/// retrievable by name without invoking the provider, so a compiled realization answers the same
/// inspection questions as an explicit-data one.
[<StructuralEquality; NoComparison>]
type BindingPlan =
    private
        { Identifier: PlanId
          Contract: ContractDocument
          AnsweringProvider: PortableProviderRef
          Catalog: ShapeCatalog
          Operations: PortableOperationRef list
          Shapes: PortableShapeRef list
          Fragments: PortableFragmentRef list
          SatisfiedRequirements: PortableDependencyRef list
          CompactAssignments: CompactAssignment list
          HostEndpoint: string
          ProviderEndpoint: string
          SelectionReason: string
          Realization: Realization
          Facts: Map<string, string> }

[<RequireQualifiedAccess>]
module BindingPlan =

    let private render (flag: bool) = if flag then "true" else "false"

    let private join (values: string seq) = System.String.Join(", ", values)

    let planId plan = plan.Identifier
    let contract plan = plan.Contract
    let catalog plan = plan.Catalog
    let contractVersion plan = plan.Contract.ContractVersion
    let component' plan = plan.Contract.Component
    /// The provider that answered, read from the offered document.
    ///
    /// Negotiation refuses a provider mismatch, so this equals the required declaration whenever a
    /// plan exists. Reading it from the offered side keeps the fact honest about who answered rather
    /// than who was asked, which is what a field of this name has to mean. Decision 11.
    let answeringProvider plan = plan.AnsweringProvider

    let provider plan = plan.AnsweringProvider
    let operations plan = plan.Operations
    let shapes plan = plan.Shapes
    let fragments plan = plan.Fragments
    let satisfiedRequirements plan = plan.SatisfiedRequirements
    let compactAssignments plan = plan.CompactAssignments
    let hostEndpoint plan = plan.HostEndpoint
    let providerEndpoint plan = plan.ProviderEndpoint
    let selectedProvider plan = plan.AnsweringProvider
    let selectionReason plan = plan.SelectionReason
    let realization plan = plan.Realization
    let limits plan = plan.Contract.Limits
    let authority plan = plan.Contract.Authority
    let trustBoundaryCrossed plan = plan.Contract.Authority.TrustBoundaryCrossed
    let representation plan = plan.Contract.Representation.Representation
    let resourceFlavors plan = plan.Contract.Representation.ResourceFlavors
    let acceptedResourceHandles plan = plan.Contract.Representation.AcceptedResourceHandles
    let replayProtectionDeclared plan = plan.Contract.Lifecycle.ReplayProtectionDeclared
    let replayWindow plan = plan.Contract.Lifecycle.ReplayWindow
    let lifecycleFeatures plan = plan.Contract.Lifecycle.Features

    /// A direct call has no wire, so its framing is the realization itself rather than the
    /// length-delimited framing the contract declares for a process seam.
    let framing plan =
        match plan.Realization with
        | Realization.FixedDirectCall -> PortableRepresentations.DirectCall
        | Realization.NegotiatedProcess -> plan.Contract.Representation.Framing

    let authorityDecisionPoint plan =
        match plan.Contract.Authority.PresentationMode with
        | AuthorityMode.CrossTrustNoCapabilityTransfer -> AuthorityDecisionPoint.ProviderDomain
        | AuthorityMode.LocalDomainEvaluated -> AuthorityDecisionPoint.TargetBoundary

    /// Cross-trust revocability is carried-tier by default; a resolver or indirection point is a
    /// declared capability, never an assumed one.
    let chainConjunctionTier _ = "carried"

    let resourceOwnership plan =
        let flavors = plan.Contract.Representation.ResourceFlavors

        if List.isEmpty flavors then "none"
        elif List.contains ResourceFlavor.CopiedImmutableBlobToken flavors then "copied"
        else "provider-retained"

    let implicitCopyPermitted _ = false

    let integrityAlgorithm plan =
        if List.contains ResourceFlavor.CopiedImmutableBlobToken plan.Contract.Representation.ResourceFlavors then
            Some "SHA-256"
        else
            None

    let crossedBoundaries plan =
        let boundaries =
            [ if plan.Realization = Realization.NegotiatedProcess then
                  "process"
              if plan.Contract.Authority.TrustBoundaryCrossed then
                  "trust" ]

        if List.isEmpty boundaries then [ "none" ] else boundaries

    let terminalStates = [ "terminated"; "failed" ]

    let failureDomainsObservable =
        FailureDomain.all |> List.map FailureDomain.token

    let exceptionTransportPermitted _ = false

    let tryFact name plan = Map.tryFind name plan.Facts

    let factNames plan =
        plan.Facts |> Map.toList |> List.map fst

    let operation reference plan : PortableResult<OperationDeclaration> =
        match ContractDocument.tryOperation reference plan.Contract with
        | Some declaration -> Ok declaration
        | None ->
            refuse
                ProtocolCategory.UnsupportedOperation
                "unknown-operation"
                $"Operation {PortableOperationRef.text reference} is outside the established contract."

    let private buildFacts plan =
        let limits = plan.Contract.Limits

        Map.ofList
            [ "planId", PlanId.value plan.Identifier
              "contractVersion", string plan.Contract.ContractVersion
              "component", PortableComponentRef.text plan.Contract.Component
              "provider", PortableProviderRef.text plan.AnsweringProvider
              "operations", join (plan.Operations |> List.map PortableOperationRef.text)
              "shapes", join (plan.Shapes |> List.map PortableShapeRef.text)
              "fragments", join (plan.Fragments |> List.map PortableFragmentRef.text)
              "satisfiedRequirements", join (plan.SatisfiedRequirements |> List.map PortableDependencyRef.text)
              "compactIdentifierAssignments",
              join (
                  plan.CompactAssignments
                  |> List.map (fun assignment ->
                      $"{IdentitySpace.token assignment.Space}:{assignment.Reference}={CompactId.value assignment.Compact}")
              )
              "hostEndpoint", plan.HostEndpoint
              "providerEndpoint", plan.ProviderEndpoint
              "selectedProvider", PortableProviderRef.text (selectedProvider plan)
              "selectionReason", plan.SelectionReason
              "authorityPresentationMode", AuthorityMode.token plan.Contract.Authority.PresentationMode
              "trustBoundaryCrossed", render plan.Contract.Authority.TrustBoundaryCrossed
              "noCapabilityTransfer", render plan.Contract.Authority.NoCapabilityTransfer
              "constraintPolicy", plan.Contract.Authority.ConstraintPolicy
              "authorityDecisionPoint", AuthorityDecisionPoint.token (authorityDecisionPoint plan)
              "chainConjunctionTier", chainConjunctionTier plan
              "representation", plan.Contract.Representation.Representation
              "framing", framing plan
              "resourceFlavors", join plan.Contract.Representation.ResourceFlavors
              "acceptedResourceHandles", join plan.Contract.Representation.AcceptedResourceHandles
              "resourceOwnership", resourceOwnership plan
              "implicitCopyPermitted", render (implicitCopyPermitted plan)
              "integrityAlgorithm", (integrityAlgorithm plan |> Option.defaultValue "absent")
              "maxConcurrentRequests", string limits.MaxConcurrentRequests
              "orderingGuarantee", "none"
              "realization", Realization.token plan.Realization
              "crossedBoundaries", join (crossedBoundaries plan)
              "limits",
              join
                  [ $"maxFrameBytes={limits.MaxFrameBytes}"
                    $"maxNestingDepth={limits.MaxNestingDepth}"
                    $"maxRecordFields={limits.MaxRecordFields}"
                    $"maxFragmentsPerRecord={limits.MaxFragmentsPerRecord}"
                    $"maxSequenceItems={limits.MaxSequenceItems}"
                    $"maxTextBytes={limits.MaxTextBytes}"
                    $"maxByteStringBytes={limits.MaxByteStringBytes}"
                    $"maxResourceBytes={limits.MaxResourceBytes}"
                    $"ioTimeoutMilliseconds={limits.IoTimeoutMilliseconds}"
                    $"maxConcurrentRequests={limits.MaxConcurrentRequests}" ]
              "replayProtectionDeclared", render plan.Contract.Lifecycle.ReplayProtectionDeclared
              "replayWindow", plan.Contract.Lifecycle.ReplayWindow
              "lifecycleFeatures",
              join (
                  plan.Contract.Lifecycle.Features
                  |> Map.toList
                  |> List.map (fun (name, declared) -> $"{name}={render declared}")
              )
              "terminalStates", join terminalStates
              "failureDomainsObservable", join failureDomainsObservable
              "exceptionTransportPermitted", render (exceptionTransportPermitted plan) ]

    /// Freezes one plan. Only negotiation calls this, because a plan that was not negotiated has no
    /// facts to be frozen from.
    let internal freeze
        (identifier: PlanId)
        (document: ContractDocument)
        (answeringProvider: PortableProviderRef)
        (catalog: ShapeCatalog)
        (operations: PortableOperationRef list)
        (shapes: PortableShapeRef list)
        (fragments: PortableFragmentRef list)
        (satisfied: PortableDependencyRef list)
        (assignments: CompactAssignment list)
        (hostEndpoint: string)
        (providerEndpoint: string)
        (selectionReason: string)
        (realization: Realization)
        =
        let plan =
            { Identifier = identifier
              Contract = document
              AnsweringProvider = answeringProvider
              Catalog = catalog
              Operations = operations
              Shapes = shapes
              Fragments = fragments
              SatisfiedRequirements = satisfied
              CompactAssignments = assignments
              HostEndpoint = hostEndpoint
              ProviderEndpoint = providerEndpoint
              SelectionReason = selectionReason
              Realization = realization
              Facts = Map.empty }

        { plan with Facts = buildFacts plan }
