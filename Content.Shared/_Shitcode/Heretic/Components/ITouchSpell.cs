using Robust.Shared.Audio;

namespace Content.Shared._Shitcode.Heretic.Components;

public interface ITouchSpell
{
    EntityUid? Action { get; set; }

    TimeSpan Cooldown { get; set; }

    LocId Speech { get; set; }

    SoundSpecifier? Sound { get; set; }
}
