using Content.Shared.Plumbing.Components;
using Content.Shared.Plumbing.EntitySystems;
using Content.Server.Audio;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Plumbing.Components;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Prototypes;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Plumbing.EntitySystems;

public sealed class PlumbingHeatExchangerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlumbingHeatExchangerComponent, PlumbingDeviceProcessEvent>(OnHeatExchangerProcessed);
    }

    private void OnHeatExchangerProcessed(Entity<PlumbingHeatExchangerComponent> entity, ref PlumbingDeviceProcessEvent args)
    {
        var heatExchangerComponent = entity.Comp;
        if (!_nodeContainerSystem.TryGetNodes(entity.Owner, heatExchangerComponent.PlumbingNodeName, heatExchangerComponent.AtmosNodeName, out PlumbingNode? inlet, out PipeNode? outlet) ||
            inlet.NetSolution is not { } solution)
            return;

        // It's that simple!
        var air = outlet.Air;
        if (MathF.Abs(air.Temperature - solution.Temperature) <= Atmospherics.MinimumTemperatureDeltaToConsider)
        {
            solution.Temperature = air.Temperature;
            return;
        }

        var dT = (air.Temperature - solution.Temperature) * heatExchangerComponent.Coefficient * args.DeltaTime;

        air.Temperature -= dT;
        solution.Temperature += dT;
    }
}
