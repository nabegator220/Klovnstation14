using Robust.Shared.Utility;
using Content.Shared._KS14.Silicons.Bots.Components;
using Content.Shared._KS14.Silicons.StationAi;
using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
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
}
