using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Client.Player;
using Robust.Shared.Timing;
using System.Threading;
using RobustTimer = Robust.Shared.Timing.Timer;

namespace Content.Client.Silicons.StationAi;

/// <summary>
/// One-shot targeting helper: when started it captures the next left-click (send) or right-click (cancel),
/// projects the screen cursor to world coordinates and invokes a callback with the NetCoordinates.
/// </summary>
[Obsolete("Targeting moved into StationAiSystem._KS14.Bot.cs. This type is retained as a no-op shim.")]
public sealed class StationAiTargetingSystem : EntitySystem
{
    public override void Initialize()
    {
        // intentionally no-op; logic moved to StationAiSystem to avoid global input bindings.
    }

    public override void Shutdown()
    {
        // no-op
    }
}
