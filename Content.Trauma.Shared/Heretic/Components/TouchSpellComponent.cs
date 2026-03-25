using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TouchSpellComponent : Component
{
    [DataField]
    public EntityUid? Action;

    [DataField]
    public TimeSpan Cooldown;

    [DataField]
    public LocId? Speech;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool CanUseOnSelf;

    [DataField]
    public bool BypassNullrod;
}
