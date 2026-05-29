// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Loudspeaker;

[ByRefEvent]
public record struct GetLoudspeakerEvent(List<EntityUid> Loudspeakers);
