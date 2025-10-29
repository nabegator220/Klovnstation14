using Robust.Shared.GameStates;

namespace Content.Shared._KS14.Silicons.Bots.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ControllableBotComponent : Component
{
    /// <summary>
    /// Test field 1
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TestField1 = true;

    /// <summary>
    /// Test field 2
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TestField2 = false;
}
