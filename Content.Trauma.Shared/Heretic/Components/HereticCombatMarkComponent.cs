// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class HereticCombatMarkComponent : BaseSpriteOverlayComponent
{
    [DataField, AutoNetworkedField]
    public HereticPath Path = HereticPath.Blade;

    [DataField]
    public TimeSpan MaxDisappearTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan DisappearTime = TimeSpan.FromSeconds(15);

    [DataField]
    public int Repetitions = 1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextDisappear;

    [DataField]
    public SoundSpecifier? TriggerSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/repulse.ogg");

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new("_Goobstation/Heretic/combat_marks.rsi"), "blade");

    public override Enum Key { get; set; } = HereticCombatMarkKey.Key;
}

public enum HereticCombatMarkKey : byte
{
    Key,
}
