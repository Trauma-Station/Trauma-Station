// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.CosmicCult;

[Serializable, NetSerializable]
public sealed partial class CosmicSiphonDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicBlankDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class AbsorbRiftDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class PurgeRiftDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StartFinaleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CancelFinaleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicColossusIngressDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicChantryDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicHibernationDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicConversionDoAfterEvent : SimpleDoAfterEvent;
