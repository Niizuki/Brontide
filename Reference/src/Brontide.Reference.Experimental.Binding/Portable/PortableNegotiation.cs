using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>
/// Negotiates two contract documents into one frozen Binding Plan.
/// </summary>
/// <remarks>
/// Every step completes before any provider activation, so a failure at any step produces zero
/// provider effects. Version 0.1 negotiates by exact reference equality: there is no structural
/// similarity matching, no nearest-version selection, and no inference of authority from provider
/// availability.
/// </remarks>
public static class PortableNegotiation
{
    private static readonly ImmutableArray<string> SupportedFeatures =
    [
        "establishment", "readiness-signal", "single-invocation", "clean-withdrawal", "clean-termination"
    ];

    private static readonly ImmutableArray<string> UnsupportedFeatures =
    [
        "retry", "cancellation", "streaming", "ordering-guarantee", "exactly-once-execution"
    ];

    public static PortableBindingPlan Negotiate(
        PortableContractDocument required,
        PortableContractDocument offered,
        PortableRealization realization,
        string hostEndpoint,
        string providerEndpoint,
        string selectionReason)
    {
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(offered);

        RequireContractVersion(required, offered);
        RequireProvider(required, offered);
        RequireComponent(required, offered);
        var satisfied = MatchRequirements(required, offered);
        MatchOperations(required, offered);
        MatchAuthority(required, offered);
        MatchRepresentation(required, offered);
        MatchLimitsAndLifecycle(required, offered);

        // Compact identifiers are assigned only once every canonical step above has succeeded.
        var operations = required.Operations.Select(operation => operation.Reference).ToImmutableArray();
        var shapes = required.Shapes.Select(shape => shape.Reference).Order().ToImmutableArray();
        var fragments = required.Fragments.Select(fragment => fragment.Reference).Order().ToImmutableArray();

        return new PortableBindingPlan(
            PortablePlanId.New(),
            required,
            offered.Provider,
            operations,
            shapes,
            fragments,
            satisfied,
            AssignCompactIdentifiers(operations, shapes, fragments),
            hostEndpoint,
            providerEndpoint,
            selectionReason,
            realization);
    }

    private static void RequireContractVersion(PortableContractDocument required, PortableContractDocument offered)
    {
        if (required.ContractVersion != offered.ContractVersion ||
            required.ContractVersion != PortableContractDocument.SupportedContractVersion)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedVersion,
                "contract-version",
                "The declared contract versions are not both the recognized version.");
        }
    }

    /// <summary>
    /// A required document naming a provider is binding, not expectational.
    /// </summary>
    /// <remarks>
    /// Version 0.1 defines no way to say "any provider of this Component", so the peer must answer as
    /// the provider the host named. Refusing the mismatch here is what lets the plan's provider facts
    /// be read from the offered document and still equal the requirement. Decision 11.
    /// </remarks>
    private static void RequireProvider(PortableContractDocument required, PortableContractDocument offered)
    {
        if (required.Provider != offered.Provider)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "provider-mismatch",
                $"Required provider {required.Provider} answered as {offered.Provider}.");
        }
    }

    private static void RequireComponent(PortableContractDocument required, PortableContractDocument offered)
    {
        if (required.Component != offered.Component)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "component-mismatch",
                $"Required component {required.Component} was not offered as the exact contract.");
        }
    }

    private static ImmutableArray<PortableDependencyReference> MatchRequirements(
        PortableContractDocument required,
        PortableContractDocument offered)
    {
        var satisfied = ImmutableArray.CreateBuilder<PortableDependencyReference>();
        foreach (var requirement in required.Requirements)
        {
            var provided = offered.Provisions.Any(provision =>
                provision.Kind == requirement.Kind &&
                provision.Reference == requirement.Reference &&
                provision.ProviderSpecific == requirement.ProviderSpecific);

            switch (requirement.Strength)
            {
                case PortableRequirementStrength.Required when !provided:
                    throw new PortableFaultException(
                        PortableProtocolCategory.UnsupportedContract,
                        "requirement-unmet",
                        $"Required {requirement.Kind.Token()} {requirement.Reference} was not offered.");
                case PortableRequirementStrength.Opposed when provided:
                    throw new PortableFaultException(
                        PortableProtocolCategory.UnsupportedContract,
                        "opposed-requirement-offered",
                        $"Opposed {requirement.Kind.Token()} {requirement.Reference} was offered; it is refused rather than ignored.");
                default:
                    break;
            }

            if (provided && requirement.Strength != PortableRequirementStrength.Opposed)
            {
                satisfied.Add(requirement.Reference);
            }
        }

        return satisfied.ToImmutable();
    }

    private static void MatchOperations(PortableContractDocument required, PortableContractDocument offered)
    {
        foreach (var operation in required.Operations)
        {
            var candidate = offered.Operations
                .FirstOrDefault(offeredOperation => offeredOperation.Reference == operation.Reference)
                ?? throw new PortableFaultException(
                    PortableProtocolCategory.UnsupportedOperation,
                    "unknown-operation",
                    $"Operation {operation.Reference} is not offered.");

            if (candidate.InputShape != operation.InputShape ||
                candidate.ResultShape != operation.ResultShape ||
                candidate.DetailShape != operation.DetailShape ||
                !candidate.RequiredFragments.SequenceEqual(operation.RequiredFragments) ||
                !candidate.ResourceFlavors.SequenceEqual(operation.ResourceFlavors, StringComparer.Ordinal))
            {
                throw new PortableFaultException(
                    PortableProtocolCategory.UnsupportedContract,
                    "operation-shape-mismatch",
                    $"Operation {operation.Reference} declares incompatible Shapes, Fragments, or resource flavors.");
            }

            foreach (var flavor in operation.ResourceFlavors)
            {
                _ = PortableResourceFlavors.Parse(flavor);
            }
        }
    }

    private static void MatchAuthority(PortableContractDocument required, PortableContractDocument offered)
    {
        PortableAuthorityGate.RequireValidPresentation(required.Authority);
        PortableAuthorityGate.RequireValidPresentation(offered.Authority);
        if (required.Authority != offered.Authority)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.InvalidAuthorityPresentation,
                "authority-mismatch",
                "The endpoints declare different authority presentations, which cannot be resolved to a permissive mode.");
        }
    }

    private static void MatchRepresentation(PortableContractDocument required, PortableContractDocument offered)
    {
        // Compared member by member: the declaration carries sequences, whose record equality is by
        // instance rather than by content.
        var equal = string.Equals(
                required.Representation.Representation,
                offered.Representation.Representation,
                StringComparison.Ordinal) &&
            string.Equals(required.Representation.Framing, offered.Representation.Framing, StringComparison.Ordinal) &&
            required.Representation.ResourceFlavors.SequenceEqual(
                offered.Representation.ResourceFlavors,
                StringComparer.Ordinal) &&
            required.Representation.AcceptedResourceHandles.SequenceEqual(
                offered.Representation.AcceptedResourceHandles,
                StringComparer.Ordinal);
        if (!equal)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "representation-mismatch",
                "The endpoints declare different payload representations.");
        }

        if (!string.Equals(
                required.Representation.Representation,
                PortableRepresentations.PortableCborCore,
                StringComparison.Ordinal))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "representation-unsupported",
                $"'{required.Representation.Representation}' is not the portable wire representation.");
        }

        if (!string.Equals(
                required.Representation.Framing,
                PortableRepresentations.LengthDelimited,
                StringComparison.Ordinal))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "framing-unsupported",
                "The portable representation is length-delimited.");
        }

        foreach (var flavor in required.Representation.ResourceFlavors)
        {
            _ = PortableResourceFlavors.Parse(flavor);
        }

        if (required.Representation.AcceptedResourceHandles.Length > 0 &&
            !required.Representation.ResourceFlavors.Contains(
                PortableResourceFlavors.AddressingOnlyHandle,
                StringComparer.Ordinal))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "handle-accept-list",
                "An accept list is declared without the addressing-only-handle flavor it governs.");
        }
    }

    private static void MatchLimitsAndLifecycle(PortableContractDocument required, PortableContractDocument offered)
    {
        if (required.Limits != offered.Limits)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "limits-mismatch",
                "The endpoints declare different delivery limits.");
        }

        required.Limits.Validate();

        if (required.Lifecycle.ReplayProtectionDeclared != offered.Lifecycle.ReplayProtectionDeclared ||
            !string.Equals(required.Lifecycle.ReplayWindow, offered.Lifecycle.ReplayWindow, StringComparison.Ordinal))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "replay-declaration-mismatch",
                "The endpoints declare different replay protection.");
        }

        foreach (var feature in SupportedFeatures)
        {
            RequireFeature(required, feature, expected: true);
            RequireFeature(offered, feature, expected: true);
        }

        foreach (var feature in UnsupportedFeatures)
        {
            RequireFeature(required, feature, expected: false);
            RequireFeature(offered, feature, expected: false);
        }
    }

    private static void RequireFeature(PortableContractDocument document, string feature, bool expected)
    {
        if (!document.Lifecycle.Features.TryGetValue(feature, out var declared))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "feature-undeclared",
                $"Lifecycle feature '{feature}' is not declared, so no consumer can tell whether it is promised.");
        }

        if (declared != expected)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                expected ? "feature-unsupported" : "feature-not-promised",
                expected
                    ? $"Lifecycle feature '{feature}' is required and must be supported."
                    : $"Lifecycle feature '{feature}' is not promised by version 0.1 and is refused rather than silently ignored.");
        }
    }

    private static ImmutableArray<PortableCompactAssignment> AssignCompactIdentifiers(
        ImmutableArray<PortableOperationReference> operations,
        ImmutableArray<PortableShapeReference> shapes,
        ImmutableArray<PortableFragmentReference> fragments)
    {
        var assignments = ImmutableArray.CreateBuilder<PortableCompactAssignment>();
        for (var index = 0; index < operations.Length; index++)
        {
            assignments.Add(new PortableCompactAssignment(
                PortableIdentitySpace.Operation,
                operations[index].ToString(),
                new PortableCompactIdentifier(index)));
        }

        for (var index = 0; index < shapes.Length; index++)
        {
            assignments.Add(new PortableCompactAssignment(
                PortableIdentitySpace.Shape,
                shapes[index].ToString(),
                new PortableCompactIdentifier(index)));
        }

        for (var index = 0; index < fragments.Length; index++)
        {
            assignments.Add(new PortableCompactAssignment(
                PortableIdentitySpace.Fragment,
                fragments[index].ToString(),
                new PortableCompactIdentifier(index)));
        }

        return assignments.ToImmutable();
    }
}
