// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Plumbing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PlumbingPumpComponent : Component
{
    public string InletName = "inlet";
    public string OutletName = "outlet";

    /// <summary>
    ///     The desired throughput of this pump in units.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Rate = 20;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ToggleTime = TimeSpan.FromSeconds(0.4);
}
