// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Plumbing.Components;

/// <summary>
///     Component for plumbing devices that transition from one state to another,
///         with a state inbetween. With an animation usually. Because we have
///         alot of stuff that needs this..
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlumbingTransitioningDeviceComponent : Component
{
    [AutoNetworkedField, DataField]
    public PlumbingDeviceState State = PlumbingDeviceState.Off;

    /// <summary>
    ///     The next time that this door will proceed to the next state,
    ///         if in an 'inbetween' state such as `ToOff` or `ToOn`.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextStateChange;

    [DataField]
    public bool ToggleOnActivate = false;

    /// <summary>
    ///     The layer affected by this.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string Layer = "default";

    [ViewVariables(VVAccess.ReadOnly)]
    public const string AnimationKey = "transitioning_device_animation";

    /// <summary>
    ///     The icon state of the selected <see cref="Layer"/>,
    ///         when this device is on. Required.
    /// </summary>
    [DataField(required: true)]
    public string OnState;

    /// <summary>
    ///     The icon state of the selected <see cref="Layer"/>,
    ///         when this device is off. Required.
    /// </summary>
    [DataField(required: true)]
    public string OffState;

    /// <summary>
    ///     The icon state of the selected <see cref="Layer"/>,
    ///         when this device is turning on. Optional.
    /// </summary>
    [DataField]
    public string? ToOnState;

    /// <summary>
    ///     The icon state of the selected <see cref="Layer"/>,
    ///         when this device is turning off. Optional.
    /// </summary>
    [DataField]
    public string? ToOffState;

    /// <summary>
    ///     The animation used when the device is turning on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public object? ToOnAnimation = default!;

    /// <summary>
    ///     The animation used when the device is turning off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public object? ToOffAnimation = default!;

    /// <summary>
    ///     The amount of time for this device to turn on.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OnLength = TimeSpan.FromSeconds(0);

    /// <summary>
    ///     The amount of time for this device to turn off.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OffLength = TimeSpan.FromSeconds(0);
}

[Serializable, NetSerializable]
public enum PlumbingDeviceState : byte // These are intentionally kept in this order.
{
    ToOff,
    Off,
    ToOn,
    On,
}

[Serializable, NetSerializable]
public enum PlumbingDeviceVisuals : byte
{
    State
}

/// <summary>
///     Raised on something with <see cref="PlumbingTransitioningDeviceComponent"/>
///         when it's state gets changed.
/// </summary>
public readonly record struct PlumbingDeviceStateChangedEvent(PlumbingDeviceState State);
