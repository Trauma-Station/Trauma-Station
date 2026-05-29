// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Goobstation.Shared.Harvestable;


[Serializable, NetSerializable]
public sealed partial class HarvestedDoAfterEvent : SimpleDoAfterEvent;
