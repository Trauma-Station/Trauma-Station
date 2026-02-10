// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 absurd-shaman <165011607+absurd-shaman@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.ReverseBearTrap;

[RegisterComponent, NetworkedComponent, Access(typeof(ReverseBearTrapSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class ReverseBearTrapComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan CountdownDuration;

    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;

    [ViewVariables]
    public bool Ticking => NextTrigger != null;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? NextTrigger;

    [DataField, AutoNetworkedField]
    public float CurrentEscapeChance;

    [DataField, AutoNetworkedField]
    public bool Struggling;

    [DataField, AutoNetworkedField]
    public EntityUid? LoopSoundStream { get; set; }

    [DataField("soundPath")]
    public SoundSpecifier LoopSound { get; set; } = new SoundPathSpecifier("/Audio/_Goobstation/Machines/clock_tick.ogg");

    [DataField("beepSoundPath")]
    public SoundSpecifier BeepSound { get; set; } = new SoundPathSpecifier("/Audio/_Goobstation/Machines/beep.ogg");

    [DataField("snapSoundPath")]
    public SoundSpecifier SnapSound { get; set; } = new SoundPathSpecifier("/Audio/_Goobstation/Effects/snap.ogg");

    [DataField]
    public SoundSpecifier StartCuffSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_start.ogg");

    [DataField]
    public List<TimeSpan>? DelayOptions = null;

    [DataField]
    public float BaseEscapeChance;

    /// <summary>
    /// Damage dealt to the user's head after welding the trap.
    /// </summary>
    [DataField]
    public DamageSpecifier WeldDamage = new()
    {
        DamageDict = new()
        {
            { "Heat", 50 }
        }
    };
}
