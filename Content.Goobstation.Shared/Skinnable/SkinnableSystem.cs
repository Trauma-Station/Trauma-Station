// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Damage;
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
    [Dependency] private readonly DamageableSystem _damageable = null!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = null!;
    [Dependency] private readonly SharedPopupSystem _popups = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkinnableComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SkinnableComponent, SkinningDoAfterEvent>(OnSkinningDoAfter);
    }

    private void OnGetVerbs(Entity<SkinnableComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess
        || !args.CanInteract
        || !args.CanComplexInteract
        || !TryComp<SharpComponent>(args.Using, out _)
        || ent.Comp.Skinned)
            return;

        var target = ent;
        var performer = args.User;
        var arguments = args;
        InteractionVerb verb = new()
        {
            Act = () => { StartSkinning(performer, target, arguments); },
            Text = Loc.GetString("skin-verb"),
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Animals/monkey.rsi"), "monkey_skinned"),
            Priority = 1,
        };

        args.Verbs.Add(verb);
    }

    private void StartSkinning(EntityUid performer, Entity<SkinnableComponent> target, GetVerbsEvent<InteractionVerb> args)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            target.Comp.SkinningDoAfterDuation,
            new SkinningDoAfterEvent(),
            target,
            target,
            args.Using
            )
        {
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            BreakOnDropItem = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

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
        ent.Comp.Skinned = true;
        Dirty(ent, ent.Comp);
        _damageable.TryChangeDamage(ent, ent.Comp.DamageOnSkinned);
        // TODO: this is awful, change the mobs base rsi instead
        _appearance.SetData(ent, ToggleableVisuals.Enabled, true);
    }
}
