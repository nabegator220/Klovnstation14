using Robust.Shared.Serialization;

namespace Content.Shared.MNET.CardReader;

[Serializable, NetSerializable]
public enum DurationSignalAccessReaderVisuals : byte
{
    State,
};


[Serializable, NetSerializable]
public enum DurationSignalAccessReaderState : byte
{
    Off = 0,
    Fail = 1,
    Success = 2,
}
