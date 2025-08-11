using Content.Shared.Plumbing.Components;
using Content.Shared.Plumbing.EntitySystems;
using Content.Server.Audio;
using Content.Server.NodeContainer.EntitySystems;

namespace Content.Server.Plumbing.EntitySystems;

public sealed class PlumbingValveSystem : SharedPlumbingValveSystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlumbingValveComponent, PlumbingDeviceStateChangedEvent>(OnPumpStateChanged);
    }

    private void OnPumpStateChanged(Entity<PlumbingValveComponent> entity, ref PlumbingDeviceStateChangedEvent args)
    {
        var valveComponent = entity.Comp;
        valveComponent.Open = args.State == PlumbingDeviceState.On;

        if (!_nodeContainerSystem.TryGetNodes(entity.Owner, valveComponent.InletName, valveComponent.OutletName, out PlumbingNode? inlet, out PlumbingNode? outlet))
            return;

        if (valveComponent.Open)
        {
            inlet.AddAlwaysReachable(outlet);
            outlet.AddAlwaysReachable(inlet);
        }
        else
        {
            inlet.RemoveAlwaysReachable(outlet);
            outlet.RemoveAlwaysReachable(inlet);
        }

        _ambientSoundSystem.SetAmbience(entity.Owner, valveComponent.Open);
    }
}
