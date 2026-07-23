namespace Content.Trauma.Common.GameTicking.Events;

[ByRefEvent]
public record struct RequestNewAntagOrCallEvacEvent(float Percent, int AliveOnSpawn, TimeSpan CountDownTime, EntProtoId AntagsToSpawn, bool CantRecall, bool EndIfUnderPercent = true);
