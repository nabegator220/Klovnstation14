// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class PlumbingSynthesizerComponent : Component
{
    public string OutletName = "outlet";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> ProducedReagent = "Water";

    /// <summary>
    ///     The rate at which the <see cref="ProducedReagent"/> is synthesized.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Rate;
}
