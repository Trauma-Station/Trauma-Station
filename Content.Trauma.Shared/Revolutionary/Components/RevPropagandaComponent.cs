// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Revolutionary.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RevPropagandaComponent : Component
{
    [DataField(required: true)]
    public TimeSpan ConversionDuration;

    [DataField]
    public bool Silent;

    [DataField]
    public bool VisibleDoAfter;

    [DataField]
    public int ConsumesCharges;

    [DataField]
    public bool ApplyFlashEffect;

    [DataField]
    public TimeSpan FlashDuration = TimeSpan.FromSeconds(4); //only used if ApplyFlashEffect is true

    [DataField]
    public float SlowToOnFlashed = 0.5f; //only used if ApplyFlashEffect is true
}

[Serializable, NetSerializable]
public sealed partial class RevPropagandaDoAfterEvent : SimpleDoAfterEvent;
