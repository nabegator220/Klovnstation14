using System;
using Robust.Shared.Utility;
using Content.Shared._KS14.Silicons.Bots.Components;
using Content.Shared._KS14.Silicons.StationAi;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Input;
using Robust.Client.Graphics;
using Robust.Shared.Log;
using Robust.Shared.Input.Binding;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Timing;
using System.Threading;
using Robust.Shared.Input;
using RobustTimer = Robust.Shared.Timing.Timer;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    // --- One-shot targeting moved here so we only register input handlers when invoked. ---
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    // Use the existing `_player` field declared in StationAiSystem.cs partial.
    // (Do not redeclare IPlayerManager here to avoid duplicate dependency errors.)

    private bool _targetingActive;
    private Action<NetCoordinates, bool>? _targetingCallback;
    private CancellationTokenSource? _timeoutCts;
    private bool _bindingsRegistered;
    private StationAiTargetingOverlay? _targetingOverlay;
    // Remember the input context we replaced so we can restore it when finished.
    private string? _previousInputContextName;

    private void InitializeBot()
    {
        SubscribeLocalEvent<ControllableBotComponent, GetStationAiRadialEvent>(OnBotGetRadial);
    }

    private void OnBotGetRadial(Entity<ControllableBotComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = ent.Comp.TestField1
                    ? new SpriteSpecifier.Rsi(_aiActionsRsi, "unbolt_door")
                    : new SpriteSpecifier.Rsi(_aiActionsRsi, "bolt_door"),
                Tooltip = ent.Comp.TestField2
                    ? Loc.GetString("bolt-open")
                    : Loc.GetString("bolt-close"),
                Event = new StationAiBotTestEvent()
            }
        );
    }

    /// <summary>
    /// Start a one-shot capture; callback receives (netCoords, cancelled).
    /// This registers the pointer bindings for the duration of the capture only.
    /// </summary>
    public void StartTargeting(Action<NetCoordinates, bool> callback)
    {
        if (_targetingActive)
            return;

        // If there's no local player, immediately cancel.
        if (_player.LocalEntity == EntityUid.Invalid)
        {
            callback?.Invoke(NetCoordinates.Invalid, true);
            return;
        }

    Logger.Info("StationAi: StartTargeting invoked");
    _targetingCallback = callback;
    _targetingActive = true;

        // Register temporary pointer bindings under this system type. We'll unregister in Finish().
        if (!_bindingsRegistered)
        {
            CommandBinds.Builder
                .Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(
            (session, coords, uid) =>
            {
            Logger.Info($"StationAi: EditorPlaceObject handler invoked. targetingActive={_targetingActive}, session={(session?.Name ?? "null")}, coords={coords}, uid={uid}");
            if (!_targetingActive) return false;

            var mouse = _input.MouseScreenPosition;
            var mapCoords = _eye.PixelToMap(mouse);
            var entityCoords = _entMan.System<SharedTransformSystem>().ToCoordinates(mapCoords);
            var netCoords = _entMan.GetNetCoordinates(entityCoords);
            Logger.Info($"StationAi: targeting selected coordinates {netCoords}");
            _targetingCallback?.Invoke(netCoords, false);
            Finish();
            return true;
            },
                    (session, coords, uid) => true, true))
                .Bind(EngineKeyFunctions.EditorCancelPlace, new PointerStateInputCmdHandler(
                    (session, coords, uid) =>
                    {
                        Logger.Info($"StationAi: EditorCancelPlace handler invoked. targetingActive={_targetingActive}, session={(session?.Name ?? "null")}, coords={coords}, uid={uid}");
                        if (!_targetingActive) return false;

                        Logger.Info("StationAi: targeting cancelled by user (right-click)");
                        _targetingCallback?.Invoke(NetCoordinates.Invalid, true);
                        Finish();
                        return true;
                    },
                    (session, coords, uid) =>
                    {
                        // release handler
                        return true;
                    }, true))
                // Also bind UIRightClick so if UI code captures EditorCancelPlace we still get a chance to cancel.
                .Bind(EngineKeyFunctions.UIRightClick, new PointerStateInputCmdHandler(
                    (session, coords, uid) =>
                    {
                        Logger.Info($"StationAi: UIRightClick handler invoked. targetingActive={_targetingActive}, session={(session?.Name ?? "null")}, coords={coords}, uid={uid}");
                        if (!_targetingActive) return false;

                        Logger.Info("StationAi: targeting cancelled by user (UI right-click)");
                        _targetingCallback?.Invoke(NetCoordinates.Invalid, true);
                        Finish();
                        return true;
                    },
                    (session, coords, uid) => true, true))
                // Also bind Escape/CloseModals to allow keyboard cancel while targeting.
                .Bind(EngineKeyFunctions.CloseModals, InputCmdHandler.FromDelegate(
                    session =>
                    {
                        Logger.Info($"StationAi: CloseModals (Escape) invoked while targeting. targetingActive={_targetingActive}, session={(session?.Name ?? "null")} ");
                        if (!_targetingActive) return;
                        _targetingCallback?.Invoke(NetCoordinates.Invalid, true);
                        Finish();
                    }, null, handle: true, outsidePrediction: true))
                .Register<StationAiSystem>();

            _bindingsRegistered = true;
        }

    // Remember and set editor context so pointer events are routed to our bound handlers.
    _previousInputContextName = _input.Contexts.ActiveContext.Name;
    _input.Contexts.SetActiveContext("editor");

        // Add a small HUD overlay to indicate targeting mode.
        if (_targetingOverlay == null)
        {
            _targetingOverlay = new StationAiTargetingOverlay();
            _overlayMgr.AddOverlay(_targetingOverlay);
        }

        // Safety timeout: auto-cancel after 60 seconds to avoid leaving input stuck.
        _timeoutCts = new CancellationTokenSource();
        RobustTimer.Spawn(TimeSpan.FromSeconds(60), () =>
        {
            if (!_targetingActive) return;
            Logger.Info("StationAi: targeting timed out");
            _targetingCallback?.Invoke(NetCoordinates.Invalid, true);
            Finish();
        }, _timeoutCts.Token);

    // Listen for local player detach so we can cancel targeting if the player disconnects / changes.
    _player.LocalPlayerDetached += OnLocalPlayerDetached;
    }

    private void Finish()
    {
    Logger.Info("StationAi: targeting finished");
        _targetingActive = false;
        _targetingCallback = null;

        // Cancel any pending timeout.
        if (_timeoutCts != null)
        {
            try
            {
                _timeoutCts.Cancel();
            }
            catch
            {
                // ignore
            }
            _timeoutCts.Dispose();
            _timeoutCts = null;
        }

        // Unregister bindings we registered earlier so we don't interfere with other systems.
        if (_bindingsRegistered)
        {
            CommandBinds.Unregister<StationAiSystem>();
            _bindingsRegistered = false;
        }

        // Remove targeting overlay if present.
        if (_targetingOverlay != null)
        {
            _overlayMgr.RemoveOverlay(_targetingOverlay);
            _targetingOverlay = null;
        }

        // Restore the previously active input context if it still exists. If it doesn't, fall back to entity context.
        if (!string.IsNullOrEmpty(_previousInputContextName) && _input.Contexts.Exists(_previousInputContextName))
        {
            try
            {
                _input.Contexts.SetActiveContext(_previousInputContextName);
            }
            catch
            {
                // If restore fails for any reason, fall back to default entity context handling.
                _inputSystem.SetEntityContextActive();
            }
        }
        else
        {
            _inputSystem.SetEntityContextActive();
        }

        _previousInputContextName = null;

        _player.LocalPlayerDetached -= OnLocalPlayerDetached;
    }

    private void OnLocalPlayerDetached(EntityUid obj)
    {
        if (!_targetingActive)
            return;
        Logger.Info("StationAi: targeting cancelled due to local player detach");
        _targetingCallback?.Invoke(NetCoordinates.Invalid, true);
        Finish();
    }
}
