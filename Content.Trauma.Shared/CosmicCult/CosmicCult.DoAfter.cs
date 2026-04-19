// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.CosmicCult;

[Serializable, NetSerializable]
public sealed partial class EventCosmicSiphonDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class EventCosmicBlankDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class EventAbsorbRiftDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class EventPurgeRiftDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StartFinaleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CancelFinaleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class EventCosmicColossusIngressDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicChantryDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicHibernationDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicConversionDoAfter : SimpleDoAfterEvent;
