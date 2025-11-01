using Content.Client.UserInterface.Controls;
using Content.Shared.Silicons.StationAi;
using Content.Shared._KS14.Silicons.StationAi;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        var ev = new GetStationAiRadialEvent();
        EntMan.EventBus.RaiseLocalEvent(Owner, ref ev);

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var buttonModels = ConvertToButtons(ev.Actions);
        _menu.SetButtons(buttonModels);
        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOptionBase> ConvertToButtons(IReadOnlyList<StationAiRadial> actions)
    {
        var models = new RadialMenuActionOptionBase[actions.Count];
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            models[i] = new RadialMenuActionOption<BaseStationAiAction>(HandleRadialMenuClick, action.Event)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(action.Sprite),
                ToolTip = action.Tooltip
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(BaseStationAiAction p)
    {
        // If this is the bot test event, start a one-shot targeting flow so the next
        // left/right click is captured. Otherwise fall back to the normal behavior.
        if (p is StationAiBotTestEvent)
        {
            var stationAi = EntMan.System<StationAiSystem>();
            stationAi.StartTargeting((coords, cancelled) =>
            {
                if (cancelled)
                {
                    // user cancelled; do nothing
                    return;
                }

                // Attach selected coordinates to the action before sending so the server receives the target.
                p.TargetCoordinates = coords;

                // After capture, continue with the normal predicted message flow.
                SendPredictedMessage(new StationAiRadialMessage { Event = p });
            });

            return;
        }

        SendPredictedMessage(new StationAiRadialMessage { Event = p });
    }
}
