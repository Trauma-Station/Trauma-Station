// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Components;

namespace Content.Trauma.Shared.Multihit;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseMultihitUserConditionEvent : HandledEntityEventArgs
{
    public EntityUid User = EntityUid.Invalid;
}

public sealed partial class MultihitUserWhitelistEvent : BaseMultihitUserConditionEvent
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField]
    public bool Blacklist;
}

public sealed partial class MultihitUserHereticEvent : BaseMultihitUserConditionEvent
{
    [DataField]
    public int MinPathStage;

    [DataField]
    public HereticPath? RequiredPath;
}
