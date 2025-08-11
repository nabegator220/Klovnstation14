using Content.Server.Plumbing.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Plumbing.EntitySystems;

public sealed class PlumbingSolutionReplicatorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingSolutionReplicatorComponent, NodeGroupsRebuilt>(OnReplicatorNodesRebuilt);
        //SubscribeLocalEvent<PlumbingSolutionReplicatorComponent, PlumbingDeviceProcessEvent>(OnProcess);
    }

    private void OnProcess(Entity<PlumbingSolutionReplicatorComponent> entity, ref PlumbingDeviceProcessEvent args)
    {
        var (owner, replicatorComponent) = entity;
        if (!_solutionContainerSystem.TryGetSolution(owner, replicatorComponent.Solution, out var solutionEntity) ||
            solutionEntity.Value.Comp.Solution is not { } solution ||
            !TryComp<AppearanceComponent>(owner, out var appearanceComponent))
            return;

        // it's called pro
        _appearanceSystem.SetData(owner, SolutionContainerVisuals.FillFraction, 1f, appearanceComponent);
        _appearanceSystem.SetData(owner, SolutionContainerVisuals.Color, solution.GetColor(_prototypeManager).WithAlpha(solution.FillFraction), appearanceComponent);
    }

    private void OnReplicatorNodesRebuilt(Entity<PlumbingSolutionReplicatorComponent> entity, ref NodeGroupsRebuilt args)
    {
        var (owner, replicatorComponent) = entity;
        if (!_nodeContainerSystem.TryGetNode(owner, replicatorComponent.NodeName, out PlumbingNode? plumbingNode) ||
            plumbingNode.NetSolution == null)
            return;

        Entity<SolutionContainerManagerComponent?> solutionContainer = owner;
        if (!_solutionContainerSystem.TryGetSolution(solutionContainer, replicatorComponent.Solution, out var solutionEntity))
            return;

        var solutionComponent = solutionEntity.Value.Comp;

        solutionComponent.Solution = plumbingNode.NetSolution;
        Dirty(solutionEntity.Value, solutionComponent);
    }
}
