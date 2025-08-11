
using Content.Maths.FixedPoint;
using Content.Server.Plumbing.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Plumbing.EntitySystems;

public sealed class PlumbingSynthesizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlumbingSynthesizerComponent, PlumbingDeviceProcessEvent>(SynthesizerProcess);
    }

    private void SynthesizerProcess(Entity<PlumbingSynthesizerComponent> entity, ref PlumbingDeviceProcessEvent args)
    {
        var (owner, synthesizerComponent) = entity;
        if (!_nodeContainerSystem.TryGetNode(owner, synthesizerComponent.OutletName, out PlumbingNode? node) ||
            node.NodeGroup is not PlumbingNet net)
            return;

        var netSolution = net.Solution;
        net.QueueInput(
            new Solution(synthesizerComponent.ProducedReagent, FixedPoint2.Min(netSolution.MaxVolume - netSolution.Volume, synthesizerComponent.Rate * args.DeltaTime))
        );
    }
}
