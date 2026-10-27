using Content.Shared.Magic.Events;
using Content.Shared.Popups;
using Content.Trauma.Shared.Magic.Events;

namespace Content.Trauma.Shared.Magic.Systems;

public sealed partial class AnimateObjectSpellSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnAnimateObjectSpell(AnimateObjectSpellEvent ev)
    {
        if (ev.Handled)
            return;

        if (TerminatingOrDeleted(ev.Target))
        {
            ev.Handled = true;
            return;
        }

        if (Transform(ev.Target).Anchored)
        {
            _popup.PopupEntity(Loc.GetString("animate-spell-anchored"), ev.Performer, ev.Performer);

            ev.Handled = true;
            return;
        }

        var componentEvent = new ChangeComponentsSpellEvent
        {
            Action = ev.Action,
            Performer = ev.Performer,
            Target = ev.Target,
            ToAdd = ev.ToAdd,
            ToRemove = ev.ToRemove,
        };

        ev.Handled = true;

        RaiseLocalEvent(componentEvent);
    }
}


