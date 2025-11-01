using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Log;

namespace Content.Client.Silicons.StationAi;

/// <summary>
/// Small screen-space overlay shown while in targeting mode.
/// Draws a short instruction and a crosshair at the mouse.
/// </summary>
public sealed class StationAiTargetingOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private Font _font = default!;

    public StationAiTargetingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _font = _res.NotoStack();
        ZIndex = 1000; // draw on top
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var uiScale = _ui.RootControl.UIScale;
    // Intentionally removed instruction text because it was obscured by other UI elements.

        // Draw a small crosshair at mouse position
        var mousePos = _ui.MousePositionScaled.Position * uiScale;
        var size = 8f;
        var half = size / 2f;
        var box = UIBox2.FromDimensions(mousePos - new Vector2(half, half), new Vector2(size, size));
        args.ScreenHandle.DrawRect(box, Color.Red, false);
    }
}
