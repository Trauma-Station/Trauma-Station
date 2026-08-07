using Content.Trauma.Common.AlertLevel;
using Robust.Shared.Prototypes;

namespace Content.Shared.AlertLevel;

public sealed partial class AlertLevelSystem
{
    public bool CanChangeTo(Entity<AlertLevelComponent> ent, ProtoId<AlertLevelPrototype> id)
    {
        var ev = new ChangeAlertLevelAttemptEvent(id, ent.Comp.CurrentAlertLevel);
        RaiseLocalEvent(ent, ref ev);
        return !ev.Cancelled;
    }
}
