// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Runes.Rending;

/// <summary>
/// Rune for sacraficing targets and summoning Nar'Sie.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CultRuneRendingComponent : Component
{
    [DataField]
    public TimeSpan SummonTime = TimeSpan.FromSeconds(40);

    [DataField]
    public SoundSpecifier FinishedDrawingAudio =
        new SoundPathSpecifier("/Audio/_Trauma/BloodCult/rending_draw_finished.ogg");

    /// <summary>
    /// Sound played to the entire server when starting to summon Nar'Sie.
    /// </summary>
    [DataField]
    public SoundSpecifier SummonAudio = new SoundPathSpecifier("/Audio/_Trauma/BloodCult/rending_ritual.ogg");

    [DataField]
    public EntProtoId NarsiePrototype = "MobNarsieSpawn";

    /// <summary>
    /// Used to track if the rune is being used right now.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Used to track the summon audio entity.
    /// </summary>
    [DataField]
    public EntityUid? AudioEntity;
}

[Serializable, NetSerializable]
public enum RendingRuneVisuals
{
    Active,
    Layer
}

[Serializable, NetSerializable]
public sealed partial class RendingRuneDoAfter : SimpleDoAfterEvent;

/// <summary>
/// Broadcast after summoning Nar'Sie.
/// </summary>
[ByRefEvent]
public record struct BloodCultNarsieSummonedEvent();
