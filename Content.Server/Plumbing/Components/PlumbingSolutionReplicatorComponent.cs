namespace Content.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class PlumbingSolutionReplicatorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "pipe";

    [DataField("node"), ViewVariables(VVAccess.ReadWrite)]
    public string NodeName = "pipe";
}
