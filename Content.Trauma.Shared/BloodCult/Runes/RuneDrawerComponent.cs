// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Runes;

/// <summary>
/// Item that allows blood cultists to draw runes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RuneDrawerComponent : Component
{
    [DataField]
    public TimeSpan EraseTime = TimeSpan.FromSeconds(4);

    [DataField]
    public SoundSpecifier StartDrawingSound = new SoundPathSpecifier("/Audio/_Trauma/BloodCult/butcher.ogg");

    [DataField]
    public SoundSpecifier EndDrawingSound = new SoundPathSpecifier("/Audio/_Trauma/BloodCult/blood.ogg");
}

[Serializable, NetSerializable]
public enum RuneDrawerBuiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class RuneDrawerSelectedMessage(ProtoId<BloodRunePrototype> rune) : BoundUserInterfaceMessage
{
    public readonly ProtoId<BloodRunePrototype> Rune = rune;
}

[Serializable, NetSerializable]
public sealed partial class RuneEraseDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class DrawRuneDoAfterEvent : DoAfterEvent
{
    public ProtoId<BloodRunePrototype> Rune;

    public DrawRuneDoAfterEvent(ProtoId<BloodRunePrototype> rune)
    {
        Rune = rune;
    }

    public override DoAfterEvent Clone()
        => new DrawRuneDoAfterEvent(Rune);
}
