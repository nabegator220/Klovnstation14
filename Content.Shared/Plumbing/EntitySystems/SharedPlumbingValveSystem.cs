// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Plumbing.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Plumbing.EntitySystems;

public abstract class SharedPlumbingValveSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPlumbingTransitioningDeviceSystem _plumbingDeviceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingValveComponent, ActivateInWorldEvent>(OnValveActivate);
        SubscribeLocalEvent<PlumbingValveComponent, ExaminedEvent>(OnValveExamined);
    }

    private void OnValveActivate(Entity<PlumbingValveComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (_plumbingDeviceSystem.TryTransitionStateToOpposite(entity.Owner))
            _audioSystem.PlayPredicted(entity.Comp.ToggleSound, entity, args.User);

        args.Handled = true;
    }

    private void OnValveExamined(Entity<PlumbingValveComponent> entity, ref ExaminedEvent args)
    {
        var valveComponent = entity.Comp;
        if (!TryComp(entity.Owner, out TransformComponent? transformComponent) ||
            !transformComponent.Anchored)
            return;

        // Copypasta Ops
        if (!Loc.TryGetString("gas-valve-system-examined", out var str,
                ("statusColor", valveComponent.Open ? "green" : "orange"),
                ("open", valveComponent.Open)))
            return;

        args.PushMarkup(str);
    }
}
