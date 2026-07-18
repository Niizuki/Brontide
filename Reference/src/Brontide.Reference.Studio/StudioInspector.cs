using System.Collections.ObjectModel;
using Brontide.Reference.Core;

namespace Brontide.Reference.Studio;

public sealed class StudioInspector
{
    public ObservableCollection<string> ActorGraph { get; } = [];
    public ObservableCollection<string> CapabilityTrees { get; } = [];
    public ObservableCollection<string> ExecutionLog { get; } = [];

    public void Refresh(AuthorityDomain domain)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ActorGraph.Clear();
        CapabilityTrees.Clear();
        ExecutionLog.Clear();

        foreach (var actor in domain.Actors)
        {
            ActorGraph.Add(actor.DisplayName);
        }

        foreach (var capability in domain.Capabilities)
        {
            ActorGraph.Add(
                $"  {capability.Holder} ──[{string.Join(", ", capability.RootOperations)}]──▶ {capability.Target}");
        }

        foreach (var capability in domain.Capabilities.Where(capability => capability.Parent is null))
        {
            AppendCapability(domain, capability, 0);
        }

        foreach (var entry in domain.Provenance)
        {
            if (entry.Kind != ProvenanceKind.Execution || entry.Execution is null)
            {
                continue;
            }

            var prefix = entry.Authorized == true ? "accepted" : "denied";
            ExecutionLog.Add(
                $"#{entry.Sequence} {prefix}: {entry.Execution.Initiator} → {entry.Execution.Operation}; {entry.Message}");
        }
    }

    private void AppendCapability(AuthorityDomain domain, Capability capability, int depth)
    {
        var constraints = capability.AddedConstraints.Length == 0
            ? string.Empty
            : $" [{string.Join(", ", capability.AddedConstraints.Select(constraint => constraint.Name))}]";
        CapabilityTrees.Add(
            $"{new string(' ', depth * 2)}{capability.Holder} → {string.Join(", ", capability.RootOperations)}{constraints}");
        foreach (var child in domain.Capabilities.Where(candidate => ReferenceEquals(candidate.Parent, capability)))
        {
            AppendCapability(domain, child, depth + 1);
        }
    }
}
