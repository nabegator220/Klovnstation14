// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Plumbing.Components;
using Content.Shared.Interaction;
using Robust.Shared.Timing;

namespace Content.Shared.Plumbing.EntitySystems;

/// <summary>
/// ⚠⚠⚠ ПРОКЛЯТИЕ 220! ⚠⚠⚠
/// </summary>
public abstract class SharedPlumbingTransitioningDeviceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingTransitioningDeviceComponent, ActivateInWorldEvent>(OnDeviceActivate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var time = _gameTiming.CurTime;
        var trDeviceQuery = EntityQueryEnumerator<PlumbingTransitioningDeviceComponent>();

        while (trDeviceQuery.MoveNext(out var uid, out var deviceComponent))
        {
            // If:
            // 1. This is the first time the tick has been predicted
            // 2. We have a time for next state change, and it has passed. This implies that the state we are in is either `ToOff` or `ToOn`.
            if (deviceComponent.NextStateChange is not { } nextStateTime ||
                time < nextStateTime)
                continue;

            Entity<PlumbingTransitioningDeviceComponent> entity = (uid, deviceComponent);
            if (!ProceedState(entity))
                Log.Warning("Device state did not progress!");
        }
    }

    /// <summary>
    ///     Proceeds a device's state to the next logical one,
    ///         e.x. from `ToOff` to `Off`, if possible.
    ///
    ///     Will process the update for the state if possible.
    /// </summary>
    /// <returns>Whether the device was updated.</returns>
    private bool ProceedState(Entity<PlumbingTransitioningDeviceComponent> device)
    {
        var component = device.Comp;

        ref PlumbingDeviceState state = ref component.State;
        component.NextStateChange = null;

        if (component.State == PlumbingDeviceState.ToOff ||
            component.State == PlumbingDeviceState.ToOn)
        {
            Log.Debug($"Cleared NSC and proceeding to state {state + 1}.");
            return SetState(device, state + 1);
        }

        Log.Error("Could not proceed to any new device state; defaulting to Off.");
        SetState(device, PlumbingDeviceState.Off);

        return false;
    }

    /// <summary>
    ///     Used to set a device to a certain state. If in an inbetween state,
    ///         and <paramref name="time"/> is specified, will change to the
    ///         next proper state after that time. Otherwise just sets it etc..
    ///         Will resolve the entity's component.
    /// </summary>
    /// <param name="ignoreCurrent">
    ///     Whether to abort and return false if the device is already at the
    ///         specified state. Defaults to false.
    /// </param>
    /// <returns>
    ///     Whether the device state was changed. Always returns false if the
    ///         entity doesn't have <see cref="PlumbingTransitioningDeviceComponent"/>.
    /// </returns>
    public bool TrySetState(Entity<PlumbingTransitioningDeviceComponent?> device, PlumbingDeviceState state, bool ignoreCurrent = false)
    {
        ref PlumbingTransitioningDeviceComponent? deviceComponent = ref device.Comp;
        if (!Resolve(device, ref deviceComponent, logMissing: false))
            return false;

        if (!ignoreCurrent && deviceComponent.State == state)
            return false;

        SetState((device.Owner, deviceComponent), state);
        return true;
    }

    /// <summary>
    ///     Basically just toggles the device.
    /// </summary>
    public bool TryTransitionStateToOpposite(Entity<PlumbingTransitioningDeviceComponent?> device)
    {
        ref PlumbingTransitioningDeviceComponent? deviceComponent = ref device.Comp;
        if (!Resolve(device, ref deviceComponent, logMissing: false))
            return false;

        PlumbingDeviceState newState;
        if (deviceComponent.State == PlumbingDeviceState.Off)
        {
            if (deviceComponent.OnLength <= TimeSpan.Zero)
                newState = PlumbingDeviceState.On;
            else
                newState = PlumbingDeviceState.ToOn;
        }
        else if (deviceComponent.State == PlumbingDeviceState.On)
        {
            if (deviceComponent.OffLength <= TimeSpan.Zero)
                newState = PlumbingDeviceState.Off;
            else
                newState = PlumbingDeviceState.ToOff;
        }
        else
            return false;

        SetState((device, deviceComponent), newState);
        return true;
    }

    /// <summary>
    ///     Sets the device's state, updating and dirtying it
    ///         accordingly.
    ///
    ///     Aborts if the device state is the same as the current
    ///         one.
    /// </summary>
    /// <returns>Returns false if the current state is the same as the specified <paramref name="state"/>, otherwise returns true.</returns>
    private bool SetState(Entity<PlumbingTransitioningDeviceComponent> device, PlumbingDeviceState state)
    {
        var component = device.Comp;
        if (component.State == state)
            return false;

        Log.Debug($"Updating state to {state}.");
        switch (state)
        {
            case PlumbingDeviceState.ToOff:
                component.NextStateChange = _gameTiming.CurTime + component.OffLength;
                Log.Debug($"Switching to inbetween of {state}, adding NSC.");
                break;

            case PlumbingDeviceState.ToOn:
                component.NextStateChange = _gameTiming.CurTime + component.OnLength;
                Log.Debug($"Switching to inbetween of {state}, adding NSC.");
                break;

            default:
                component.NextStateChange = null;
                Log.Debug($"Switching from non-inbetween to {state}, clearing NSC.");
                break;
        }

        component.State = state;

        Dirty(device.AsNullable());
        RaiseLocalEvent(device, new PlumbingDeviceStateChangedEvent(state));

        AppearanceSystem.SetData(device, PlumbingDeviceVisuals.State, state);

        return true;
    }

    private void OnDeviceActivate(Entity<PlumbingTransitioningDeviceComponent> device, ref ActivateInWorldEvent args)
    {
        if (!device.Comp.ToggleOnActivate || args.Handled || !args.Complex)
            return;

        TryTransitionStateToOpposite(device.AsNullable());
        args.Handled = true;
    }
}
