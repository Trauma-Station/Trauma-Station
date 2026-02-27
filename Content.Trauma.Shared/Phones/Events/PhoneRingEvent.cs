using Content.Trauma.Shared.Phones.Components;

namespace Content.Trauma.Shared.Phones.Events;

public sealed class PhoneRingEvent : EntityEventArgs
{
    public EntityUid phone { get; }
    public RotaryPhoneComponent otherPhoneComponent { get; }
    public PhoneRingEvent(EntityUid IncomingPhone,  RotaryPhoneComponent otherPhoneComp)
    {
        phone = IncomingPhone;
        otherPhoneComponent = otherPhoneComp;
    }
}
