namespace Content.Trauma.Shared.AntiTamper;

[Serializable, NetSerializable]
public enum AntiTamperWireActionKey : byte
{
    Key,
    Status,
    Pulsed,
    PulseCancel
}
