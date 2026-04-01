// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Wizard;

[Serializable, NetSerializable]
public record struct AfterMindSwappedEvent(EntityUid Performer, EntityUid Target);
