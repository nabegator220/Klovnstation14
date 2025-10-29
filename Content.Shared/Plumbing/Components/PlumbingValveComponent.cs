// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Plumbing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlumbingValveComponent : Component
{
    public string InletName = "inlet";
    public string OutletName = "outlet";

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Open = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ToggleTime = TimeSpan.FromSeconds(0.4);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ToggleSound;
}
