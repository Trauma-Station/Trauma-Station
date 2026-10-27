using Content.Shared.Actions;

namespace Content.Trauma.Shared.Magic.Events;

public sealed partial class AnimateObjectSpellEvent : EntityTargetActionEvent
{
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry ToAdd = new();

    [DataField]
    [AlwaysPushInheritance]
    public HashSet<string> ToRemove = new();
}
