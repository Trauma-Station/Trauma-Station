using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic;

public interface ITouchSpell
{
    EntityUid? Action { get; set; }

    TimeSpan Cooldown { get; set; }

    LocId Speech { get; set; }

    SoundSpecifier? Sound { get; set; }
}
