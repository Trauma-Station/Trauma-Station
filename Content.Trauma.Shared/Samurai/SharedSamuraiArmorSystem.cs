
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.Samurai;

/// <summary>
///
/// </summary>
public abstract partial class SharedSamuraiArmorSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedSamuraiSystem _samurai = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<SamuraiArmorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var user = args.Wearer;
        if (_samurai.SamuraiQuery.TryComp(user, out var samurai))
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
        if (!_samurai.IsSamurai(args.User))
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
        if (_samurai.SamuraiQuery.TryComp(user, out var samurai))
            UserUnequippedSuit(ent, (user, samurai));
    }

    protected virtual void SamuraiEquipped(Entity<SamuraiArmorComponent> ent, Entity<SamuraiComponent> user)
    {
        // mark the user as wearing this suit, used when being attacked among other things
        _samurai.AssignSuit(user, ent);
    }

    /// <summary>
    /// Called when a suit is unequipped, not necessarily by a space ninja.
    /// In the future it might be changed to also have explicit deactivation via toggle.
    /// </summary>
    protected virtual void UserUnequippedSuit(Entity<SamuraiArmorComponent> ent, Entity<SamuraiComponent> user)
    {
        // mark the user as not wearing a suit
        _samurai.AssignSuit(user, null);
    }
}
