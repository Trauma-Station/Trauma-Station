// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Body;

public sealed class InstallableOrganSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstallableOrganComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<InstallableOrganComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<InstallableOrganComponent, InstallOrganDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(Entity<InstallableOrganComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<BodyComponent>(args.Examiner))
            return;

        args.PushMarkup("You could easily [bold]use[/bold] this to install it into your body");
    }

    private void OnUseInHand(Entity<InstallableOrganComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            !HasComp<BodyComponent>(user) ||
            _body.GetCategory(ent.Owner) is not {} category)
            return;

        if (_body.GetOrgan(user, category) != null)
        {
            _popup.PopupClient($"You already have a {_proto.Index(category).Name}!",
                user, user, PopupType.SmallCaution);
            return;
        }

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.Delay,
            new InstallOrganDoAfterEvent(),
            ent,
            used: ent,
            target: user)
        {
            BreakOnDamage = true, // it's brain surgery basically :)
            BreakOnMove = true
        });
    }

    private void OnDoAfter(Entity<InstallableOrganComponent> ent, ref InstallOrganDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || !_body.InsertOrgan(user, ent.Owner))
            return;

        _popup.PopupClient($"You inserted the {Name(ent)} into your body.",
            user, user, PopupType.Medium);
    }
}

[Serializable, NetSerializable]
public sealed partial class InstallOrganDoAfterEvent : SimpleDoAfterEvent;
