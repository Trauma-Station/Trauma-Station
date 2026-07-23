// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.GameTicking.Events;

[ByRefEvent]
public record struct RequestNewAntagOrCallEvacEvent(float Percent, int AliveOnSpawn, TimeSpan CountDownTime, EntProtoId AntagsToSpawn, bool CantRecall, bool EndIfUnderPercent = true);
