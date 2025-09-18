using Content.Shared.DeviceLinking;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.MNET.CardReader;

/// <summary>
/// Component that, after an interaction doafter, sends a devicenet signal based on access of the user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class DurationSignalAccessReaderComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextStateChange = TimeSpan.Zero;

    /// <summary>
    /// Amount of time upon a state change to an active state (Fail/Success) before transitioning to inactive state (Off).
    /// </summary>
    [DataField]
    public TimeSpan StateChangeTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public DurationSignalAccessReaderState CurrentState = DurationSignalAccessReaderState.Off;

    /// <summary>
    /// Can DoorBumpOpeners colliding with this in-air, interact with it?
    /// </summary>
    [DataField]
    public bool BumpAccessible = true;

    /// <summary>
    /// Length of the DoAfter to interact with this.
    /// </summary>
    [DataField]
    public TimeSpan InteractionLength = TimeSpan.FromSeconds(0.3);

    /// <summary>
    /// Chance that the user will have to repeat the interaction doafter.
    /// </summary>
    [DataField]
    public float RepeatChance = 0.5f;

    /// <summary>
    /// Popup text shown to the user when they have to repeat the interaction.
    /// <see cref="RepeatPopupOthers"/> isn't shown if this is null. Why? I'm lazy.
    /// </summary>
    [DataField]
    public string? RepeatPopupSelf = "durationaccessreader-fumble-self";

    /// <summary>
    /// Popup text shown to people other than the user when they have to repeat the interaction.
    /// Is not shown if <see cref="RepeatPopupSelf"/> is null. Why? I'm lazy.
    /// </summary>
    [DataField]
    public string? RepeatPopupOthers = "durationaccessreader-fumble-others";

    #region Signals
    /// <summary>
    /// Port triggered upon failed interaction.
    /// </summary>
    [DataField, ViewVariables]
    public ProtoId<SourcePortPrototype> FailurePort = "Off";

    /// <summary>
    /// Port triggered upon successful interaction.
    /// </summary>
    [DataField, ViewVariables]
    public ProtoId<SourcePortPrototype> SuccessPort = "On";

    #endregion
    #region Sound
    /// <summary>
    /// Sound played upon starting interaction.
    /// </summary>
    [DataField, ViewVariables]
    public SoundSpecifier? StartSound;

    /// <summary>
    /// Sound played upon failed interaction.
    /// </summary>
    [DataField, ViewVariables]
    public SoundSpecifier? FailureSound;

    /// <summary>
    /// Sound played upon successful interaction.
    /// </summary>
    [DataField, ViewVariables]
    public SoundSpecifier? SuccessSound;
    #endregion
}

[Serializable, NetSerializable]
public sealed partial class DurationSignalAccessReaderDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
