// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Revolutionary;

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

    /// <summary>
    /// Whitelist checked against the headrev.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;

    [DataField]
    public EntityWhitelist? UserBlacklist;

    /// <summary>
    /// Whitelist checked against the target mob.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField(required: true)]
    public EntityWhitelist Blacklist = default!;
}

[Serializable, NetSerializable]
public sealed partial class RevPropagandaDoAfterEvent : SimpleDoAfterEvent;
