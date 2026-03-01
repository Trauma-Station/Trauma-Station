using Content.Trauma.Shared.Phones.Components;

namespace Content.Trauma.Shared.Phones.Events;

[ByRefEvent]
public record struct PhoneRingEvent
{
    public Entity<RotaryPhoneComponent> ent { get; }
    public PhoneRingEvent(Entity<RotaryPhoneComponent> Ent)
    {
        ent = Ent;
    }
}
