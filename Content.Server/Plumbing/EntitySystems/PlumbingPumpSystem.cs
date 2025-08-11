using Content.Maths.FixedPoint;
using Content.Shared.Plumbing.Components;
using Content.Server.Plumbing.Extensions;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Plumbing.EntitySystems;

public sealed class PlumbingPumpSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingPumpComponent, PlumbingDeviceProcessEvent>(PumpProcess);
        SubscribeLocalEvent<PlumbingPumpComponent, PlumbingDeviceStateChangedEvent>(OnPumpStateChanged);
    }

    private void PumpProcess(Entity<PlumbingPumpComponent> entity, ref PlumbingDeviceProcessEvent args)
    {
        var (owner, pumpComponent) = entity;
        if (!pumpComponent.Enabled ||
            !_powerReceiverSystem.IsPowered(entity) ||
            !_nodeContainerSystem.TryGetNodes(owner, pumpComponent.InletName, pumpComponent.OutletName, out PlumbingNode? inletNode, out PlumbingNode? outletNode) ||
            inletNode.NodeGroup is not PlumbingNet inputNet ||
            outletNode.NodeGroup is not PlumbingNet outputNet)
            return;

        // We can't pull more than is in the input net, or more than how much is in the output net.
        var pulledVolume = FixedPoint2.Min(inputNet.Solution.Volume, pumpComponent.Rate, outputNet.AvailableVolume);
        if (pulledVolume <= FixedPoint2.Zero)
            return;

        var taken = inputNet.Solution.CopySplitSolution(pulledVolume, _prototypeManager);
        inputNet.QueueTransfer(taken, outputNet);
    }

    private void OnPumpStateChanged(Entity<PlumbingPumpComponent> entity, ref PlumbingDeviceStateChangedEvent args)
    {
        entity.Comp.Enabled = args.State == PlumbingDeviceState.On;
    }
}
