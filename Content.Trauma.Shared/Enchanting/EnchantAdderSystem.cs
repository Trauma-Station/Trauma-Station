// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Goobstation.Shared.Enchanting.Systems;

namespace Content.Trauma.Shared.Enchanting;

public sealed partial class EnchantAdderSystem : EntitySystem
{
    [Dependency] private EnchanterSystem _enchanter = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnchantAdderComponent, BeforeRangedInteractEvent>(OnInteractUsing);
        SubscribeLocalEvent<EnchantAdderComponent, EnchantAdderDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(Entity<EnchantAdderComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach ||
            args.Target is not { } target ||
            !_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.Delay,
            new EnchantAdderDoAfterEvent(),
            eventTarget: ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        });
    }

    private void OnDoAfter(Entity<EnchantAdderComponent> ent, ref EnchantAdderDoAfterEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!_enchanter.AddEnchant(target, ent.Comp.Enchant))
            return;

        var user = args.User;
        var name = Name(target);

        var msg = $"You inscribe the {name} with magical ink...";
        _popup.PopupEntity(msg, target, user);
        PredictedQueueDel(ent);

        _meta.SetEntityName(target, ent.Comp.Name);
        _meta.SetEntityDescription(target, ent.Comp.Desc);
    }
}

[Serializable, NetSerializable]
public sealed partial class EnchantAdderDoAfterEvent : SimpleDoAfterEvent;
