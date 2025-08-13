namespace Content.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class PlumbingHeatExchangerComponent : Component
{
    [DataField]
    public string PlumbingNodeName = "inlet";
    [DataField]
    public string AtmosNodeName = "moderator";

    /// <summary>
    ///     Multiplier of how much heat is transferred. Don't let this be more than 1.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Coefficient = 0.2f;
}
