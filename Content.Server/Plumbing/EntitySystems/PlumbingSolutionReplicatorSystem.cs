using Content.Server.Plumbing.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.Plumbing.EntitySystems;

public sealed class PlumbingSolutionReplicatorSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingSolutionReplicatorComponent, NodeGroupsRebuilt>(OnReplicatorNodesRebuilt);
    }

    private void OnReplicatorNodesRebuilt(Entity<PlumbingSolutionReplicatorComponent> entity, ref NodeGroupsRebuilt args)
    {
        var (owner, replicatorComponent) = entity;
        if (!_nodeContainerSystem.TryGetNode(owner, replicatorComponent.NodeName, out PlumbingNode? plumbingNode) ||
            plumbingNode.NetSolution is not { } netSolution)
            return;

        Entity<SolutionContainerManagerComponent?> solutionContainer = owner;
        if (!_solutionContainerSystem.TryGetSolution(solutionContainer, replicatorComponent.Solution, out var solutionEntity))
            return;

        var solutionComponent = solutionEntity.Value.Comp;

        solutionComponent.Solution = netSolution;
        Dirty(solutionEntity.Value, solutionComponent);
    }
}
