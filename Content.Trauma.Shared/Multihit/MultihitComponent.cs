// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Multihit;

[RegisterComponent, NetworkedComponent]
public sealed partial class MultihitComponent : Component
{
    [DataField]
    public float DamageMultiplier = 0.67f;

    [DataField]
    public TimeSpan MultihitDelay = TimeSpan.FromSeconds(0.25);

    [DataField]
    public TimeSpan DelayPenalty = TimeSpan.Zero;

    [DataField]
    public EntityWhitelist? MultihitWhitelist;

    [DataField]
    public List<BaseMultihitUserConditionEvent> Conditions = new();

    [DataField]
    public bool RequireAllConditions;
}
