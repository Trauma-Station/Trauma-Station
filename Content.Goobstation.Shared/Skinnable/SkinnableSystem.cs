// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.Skinnable;

public sealed class SkinnableSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkinnableComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SkinnableComponent, SkinningDoAfterEvent>(OnSkinningDoAfter);
    }

    private void OnGetVerbs(Entity<SkinnableComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess ||
            !args.CanInteract ||
            !args.CanComplexInteract ||
            ent.Comp.Skinned ||
            args.Using is not {} used ||
            !HasComp<SharpComponent>(used))
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb()
        {
            Act = () => { StartSkinning(user, ent, used); },
            Text = Loc.GetString("skin-verb"),
            Icon = new SpriteSpecifier.Rsi(new("/Textures/_Trauma/Mobs/Animals/skinnedmonkey.rsi"), "monkey"),
            Priority = 1,
        });
    }

    private void StartSkinning(EntityUid performer, Entity<SkinnableComponent> target, EntityUid used)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            target.Comp.SkinningDoAfterDuation,
            new SkinningDoAfterEvent(),
            target,
            target,
            used)
        {
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            BreakOnDropItem = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        _audio.PlayPvs(target.Comp.SkinSound, target);
        var popup = Loc.GetString("skinning-start", ("target", target), ("performer", performer));
        _popups.PopupPredicted(popup, target, performer, PopupType.LargeCaution);
    }

    private void OnSkinningDoAfter(Entity<SkinnableComponent> target, ref SkinningDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target != target.Owner)
            return;

        Skin(target);
    }

    private void Skin(Entity<SkinnableComponent> ent)
    {
        if (ent.Comp.Skinned)
            return;

        ent.Comp.Skinned = true;
        Dirty(ent, ent.Comp);
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.DamageOnSkinned);
        // mfw no api :face_holding_back_tears:
        foreach (var organ in _body.GetOrgans<VisualOrganComponent>(ent.Owner))
        {
            if (organ.Comp.Data.RsiPath != ent.Comp.UnskinnedSprite)
                continue;
            organ.Comp.Data.RsiPath = ent.Comp.SkinnedSprite;
            Dirty(organ);
        }
    }
}
