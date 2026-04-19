// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Revolutionary.Components;

[Serializable, NetSerializable]
public sealed partial class RevolutionaryConverterDoAfterEvent : SimpleDoAfterEvent
{
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevolutionaryConverterComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan ConversionDuration { get; set; }

    [DataField, AutoNetworkedField]
    public bool Silent { get; set; }

    [DataField, AutoNetworkedField]
    public bool VisibleDoAfter { get; set; }

    [DataField, AutoNetworkedField]
    public int ConsumesCharges { get; set; }

    [DataField, AutoNetworkedField]
    public bool ApplyFlashEffect { get; set; }

    [DataField, AutoNetworkedField]
    public TimeSpan FlashDuration { get; set; } = TimeSpan.FromSeconds(4); //only used if ApplyFlashEffect is true

    [DataField, AutoNetworkedField]
    public float SlowToOnFlashed = 0.5f; //only used if ApplyFlashEffect is true
}
