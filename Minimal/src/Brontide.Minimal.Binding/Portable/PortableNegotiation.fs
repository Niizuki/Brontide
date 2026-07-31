namespace Brontide.Minimal.Binding.Portable

/// Negotiates two contract documents into one frozen Binding Plan.
///
/// Every step completes before any provider activation, so a failure at any step produces zero
/// provider effects. Version 0.1 negotiates by exact reference equality: there is no structural
/// similarity matching, no nearest-version selection, and no inference of authority from provider
/// availability.
[<RequireQualifiedAccess>]
module PortableNegotiation =

    let private supportedFeatures =
        [ "establishment"; "readiness-signal"; "single-invocation"; "clean-withdrawal"; "clean-termination" ]

    let private unsupportedFeatures =
        [ "retry"; "cancellation"; "streaming"; "ordering-guarantee"; "exactly-once-execution" ]

    let private requireContractVersion (required: ContractDocument) (offered: ContractDocument) =
        ensure
            (required.ContractVersion = offered.ContractVersion
             && required.ContractVersion = ContractDocument.SupportedContractVersion)
            (fun () ->
                refuse
                    ProtocolCategory.UnsupportedVersion
                    "contract-version"
                    "The declared contract versions are not both the recognized version.")

    /// A required document naming a provider is binding, not expectational.
    ///
    /// Version 0.1 defines no way to say "any provider of this Component", so the peer must answer as
    /// the provider the host named. Refusing the mismatch here is what lets the plan's provider facts
    /// be read from the offered document and still equal the requirement. Decision 11.
    let private requireProvider (required: ContractDocument) (offered: ContractDocument) =
        ensure (required.Provider = offered.Provider) (fun () ->
            unsupportedContract
                "provider-mismatch"
                $"Required provider {PortableProviderRef.text required.Provider} answered as {PortableProviderRef.text offered.Provider}.")

    let private requireComponent (required: ContractDocument) (offered: ContractDocument) =
        ensure (required.Component = offered.Component) (fun () ->
            unsupportedContract
                "component-mismatch"
                $"Required component {PortableComponentRef.text required.Component} was not offered as the exact contract.")

    let private matchRequirements (required: ContractDocument) (offered: ContractDocument) =
        let isProvided (requirement: Requirement) =
            offered.Provisions
            |> List.exists (fun provision ->
                provision.Kind = requirement.Kind
                && provision.Reference = requirement.Reference
                && provision.ProviderSpecific = requirement.ProviderSpecific)

        let rec walk satisfied remaining =
            match remaining with
            | [] -> Ok(List.rev satisfied)
            | (requirement: Requirement) :: tail ->
                let provided = isProvided requirement

                match requirement.Strength with
                | RequirementStrength.Required when not provided ->
                    unsupportedContract
                        "requirement-unmet"
                        $"Required {DependencyKind.token requirement.Kind} {PortableDependencyRef.text requirement.Reference} was not offered."
                | RequirementStrength.Opposed when provided ->
                    unsupportedContract
                        "opposed-requirement-offered"
                        $"Opposed {DependencyKind.token requirement.Kind} {PortableDependencyRef.text requirement.Reference} was offered; it is refused rather than ignored."
                | RequirementStrength.Opposed -> walk satisfied tail
                | RequirementStrength.Required
                | RequirementStrength.Preferred
                | RequirementStrength.Optional ->
                    walk (if provided then requirement.Reference :: satisfied else satisfied) tail

        walk [] required.Requirements

    let private matchOperations (required: ContractDocument) (offered: ContractDocument) =
        required.Operations
        |> iterate (fun operation ->
            match ContractDocument.tryOperation operation.Reference offered with
            | None ->
                refuse
                    ProtocolCategory.UnsupportedOperation
                    "unknown-operation"
                    $"Operation {PortableOperationRef.text operation.Reference} is not offered."
            | Some candidate ->
                portable {
                    do!
                        ensure (candidate = operation) (fun () ->
                            unsupportedContract
                                "operation-shape-mismatch"
                                $"Operation {PortableOperationRef.text operation.Reference} declares incompatible Shapes, Fragments, or resource flavors.")

                    do! operation.ResourceFlavors |> iterate (ResourceFlavor.tryParse >> Result.map ignore)
                })

    let private matchAuthority (required: ContractDocument) (offered: ContractDocument) =
        portable {
            do! PortableAuthorityGate.requireValidPresentation required.Authority
            do! PortableAuthorityGate.requireValidPresentation offered.Authority

            do!
                ensure (required.Authority = offered.Authority) (fun () ->
                    invalidAuthority
                        "authority-mismatch"
                        "The endpoints declare different authority presentations, which cannot be resolved to a permissive mode.")
        }

    let private matchRepresentation (required: ContractDocument) (offered: ContractDocument) =
        portable {
            do!
                ensure (required.Representation = offered.Representation) (fun () ->
                    unsupportedContract "representation-mismatch" "The endpoints declare different payload representations.")

            do!
                ensure (required.Representation.Representation = PortableRepresentations.PortableCborCore) (fun () ->
                    unsupportedContract
                        "representation-unsupported"
                        $"'{required.Representation.Representation}' is not the portable wire representation.")

            do!
                ensure (required.Representation.Framing = PortableRepresentations.LengthDelimited) (fun () ->
                    unsupportedContract "framing-unsupported" "The portable representation is length-delimited.")

            do! required.Representation.ResourceFlavors |> iterate (ResourceFlavor.tryParse >> Result.map ignore)

            do!
                ensure
                    (List.isEmpty required.Representation.AcceptedResourceHandles
                     || List.contains ResourceFlavor.AddressingOnlyHandleToken required.Representation.ResourceFlavors)
                    (fun () ->
                        unsupportedContract
                            "handle-accept-list"
                            "An accept list is declared without the addressing-only-handle flavor it governs.")
        }

    let private requireFeature (document: ContractDocument) feature expected =
        match Map.tryFind feature document.Lifecycle.Features with
        | None ->
            unsupportedContract
                "feature-undeclared"
                $"Lifecycle feature '{feature}' is not declared, so no consumer can tell whether it is promised."
        | Some declared when declared <> expected ->
            if expected then
                unsupportedContract "feature-unsupported" $"Lifecycle feature '{feature}' is required and must be supported."
            else
                unsupportedContract
                    "feature-not-promised"
                    $"Lifecycle feature '{feature}' is not promised by version 0.1 and is refused rather than silently ignored."
        | Some _ -> Ok()

    let private matchLimitsAndLifecycle (required: ContractDocument) (offered: ContractDocument) =
        portable {
            do!
                ensure (required.Limits = offered.Limits) (fun () ->
                    unsupportedContract "limits-mismatch" "The endpoints declare different delivery limits.")

            do! PortableLimits.validate required.Limits

            do!
                ensure
                    (required.Lifecycle.ReplayProtectionDeclared = offered.Lifecycle.ReplayProtectionDeclared
                     && required.Lifecycle.ReplayWindow = offered.Lifecycle.ReplayWindow)
                    (fun () ->
                        unsupportedContract "replay-declaration-mismatch" "The endpoints declare different replay protection.")

            for feature in supportedFeatures do
                do! requireFeature required feature true
                do! requireFeature offered feature true

            for feature in unsupportedFeatures do
                do! requireFeature required feature false
                do! requireFeature offered feature false
        }

    /// Compact identifiers are assigned only once every canonical step has succeeded, so a compact
    /// identifier can never precede the negotiation that gives it meaning.
    let private assignCompactIdentifiers operations shapes fragments =
        let assign space text index =
            CompactId.tryCreate index
            |> Result.map (fun compact ->
                { Space = space
                  Reference = text
                  Compact = compact })

        portable {
            let! operationAssignments =
                operations
                |> List.mapi (fun index reference -> IdentitySpace.Operation, PortableOperationRef.text reference, index)
                |> traverse (fun (space, text, index) -> assign space text index)

            let! shapeAssignments =
                shapes
                |> List.mapi (fun index reference -> IdentitySpace.Shape, PortableShapeRef.text reference, index)
                |> traverse (fun (space, text, index) -> assign space text index)

            let! fragmentAssignments =
                fragments
                |> List.mapi (fun index reference -> IdentitySpace.Fragment, PortableFragmentRef.text reference, index)
                |> traverse (fun (space, text, index) -> assign space text index)

            return operationAssignments @ shapeAssignments @ fragmentAssignments
        }

    let negotiate
        (required: ContractDocument)
        (offered: ContractDocument)
        (realization: Realization)
        (hostEndpoint: string)
        (providerEndpoint: string)
        (selectionReason: string)
        : PortableResult<BindingPlan> =
        portable {
            do! requireContractVersion required offered
            do! requireProvider required offered
            do! requireComponent required offered
            let! satisfied = matchRequirements required offered
            do! matchOperations required offered
            do! matchAuthority required offered
            do! matchRepresentation required offered
            do! matchLimitsAndLifecycle required offered

            let! catalog = ShapeCatalog.fromContract required
            let operations = required.Operations |> List.map (fun operation -> operation.Reference)
            let shapes = required.Shapes |> List.map (fun shape -> shape.Reference) |> List.sort
            let fragments = required.Fragments |> List.map (fun fragment -> fragment.Reference) |> List.sort
            let! assignments = assignCompactIdentifiers operations shapes fragments

            return
                BindingPlan.freeze
                    (PlanId.next ())
                    required
                    offered.Provider
                    catalog
                    operations
                    shapes
                    fragments
                    satisfied
                    assignments
                    hostEndpoint
                    providerEndpoint
                    selectionReason
                    realization
        }
