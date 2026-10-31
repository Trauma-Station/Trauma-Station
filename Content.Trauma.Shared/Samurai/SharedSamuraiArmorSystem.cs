// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;

namespace Content.Trauma.Shared.Samurai;

/// <summary>
/// Handles quipping and unequipping, as well as some API functions.
/// </summary>
public abstract partial class SharedSamuraiArmorSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] protected SamuraiSystem Samurai = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<SamuraiArmorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var user = args.Wearer;
        if (Samurai.SamuraiQuery.TryComp(user, out var samurai))
            SamuraiEquipped(ent, (user, samurai));
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SamuraiArmorComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        _actionContainer.EnsureAction(uid, ref comp.RecallEnergyPolearmActionEntity, comp.RecallEnergyPolearmAction);
        Dirty(uid, comp);
    }

    /// <summary>
    /// Add all the actions when a suit is equipped by a samurai.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnGetItemActions(Entity<SamuraiArmorComponent> ent, ref GetItemActionsEvent args)
    {
        if (!Samurai.IsSamurai(args.User))
            return;

        var comp = ent.Comp;
        args.AddAction(ref comp.RecallEnergyPolearmActionEntity, comp.RecallEnergyPolearmAction);
    }

    /// <summary>
    /// Call the shared and serverside code for when anyone unequips a suit.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnequipped(Entity<SamuraiArmorComponent> ent, ref GotUnequippedEvent args)
    {
        var user = args.EquipTarget;
        if (Samurai.SamuraiQuery.TryComp(user, out var samurai))
            UserUnequippedSuit(ent, (user, samurai));
    }

    protected virtual void SamuraiEquipped(Entity<SamuraiArmorComponent> ent, Entity<SamuraiComponent> user)
    {
        // mark the user as wearing this suit, used when being attacked among other things
        Samurai.AssignSuit(user, ent);
    }

    /// <summary>
    /// Called when a suit is unequipped, not necessarily by a space ninja.
    /// In the future it might be changed to also have explicit deactivation via toggle.
    /// </summary>
    protected virtual void UserUnequippedSuit(Entity<SamuraiArmorComponent> ent, Entity<SamuraiComponent> user)
    {
        // mark the user as not wearing a suit
        Samurai.AssignSuit(user, null);
    }
}
