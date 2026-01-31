// <Trauma>
using Content.Goobstation.Common.Traitor;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Tag;
// </Trauma>
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Uplink;

// goobstation - heavily edited. fuck newstore
// do not touch unless you want to shoot yourself in the leg
public sealed class UplinkSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private readonly GoobCommonUplinkSystem _goobUplink = default!;
    // </Trauma>
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public static readonly ProtoId<CurrencyPrototype> TelecrystalCurrencyPrototype = "Telecrystal";
    private static readonly EntProtoId FallbackUplinkImplant = "UplinkImplant";
    private static readonly ProtoId<ListingPrototype> FallbackUplinkCatalog = "UplinkUplinkImplanter";

    /// <summary>
    /// Adds an uplink to the target based on their preference (PDA, Pen, or Implant).
    /// </summary>
    /// <param name="user">The person who is getting the uplink</param>
    /// <param name="balance">The amount of currency on the uplink.</param>
    /// <param name="preference">The preferred uplink location. Defaults to PDA.</param>
    /// <returns>Whether or not the uplink was added successfully</returns>
    public bool AddUplink(EntityUid user, FixedPoint2 balance, UplinkPreference preference = UplinkPreference.Pda)
    {
        EntityUid? uplinkEntity = null;
        var isPenUplink = false;

        switch (preference)
        {
            case UplinkPreference.Pda:
                uplinkEntity = FindPdaUplinkTarget(user);
                break;
            case UplinkPreference.Pen:
                uplinkEntity = _goobUplink.FindPenUplinkTarget(user);
                isPenUplink = uplinkEntity != null;
                break;
            case UplinkPreference.Implant:
                return ImplantUplink(user, balance);
        }

        if (uplinkEntity == null)
            return ImplantUplink(user, balance);

        EnsureComp<UplinkComponent>(uplinkEntity.Value);
        SetUplink(user, uplinkEntity.Value, balance);

        if (isPenUplink)
            _goobUplink.SetupPenUplink(uplinkEntity.Value);

        return true;
    }

    /// <summary>
    /// Legacy method for backwards compatibility.
    /// Adds an uplink to the target, auto-detecting location (prefers PDA).
    /// </summary>
    public bool AddUplinkAutoDetect(EntityUid user, FixedPoint2 balance, EntityUid? uplinkEntity = null)
    {
        uplinkEntity ??= FindUplinkTarget(user);

        if (uplinkEntity == null)
            return ImplantUplink(user, balance);

        EnsureComp<UplinkComponent>(uplinkEntity.Value);
        SetUplink(user, uplinkEntity.Value, balance);

        return true;
    }

    /// <summary>
    /// Configure TC for the uplink
    /// </summary>
    private void SetUplink(EntityUid user, EntityUid uplink, FixedPoint2 balance)
    {
        if (!_mind.TryGetMind(user, out var mind, out _))
            return;

        var store = EnsureComp<StoreComponent>(uplink);

        store.AccountOwner = mind;

        store.Balance.Clear();
        var bal = new Dictionary<string, FixedPoint2> { { TelecrystalCurrencyPrototype, balance } };
        _store.TryAddCurrency(bal, uplink, store);
    }

    /// <summary>
    /// Implant an uplink as a fallback measure if the traitor had no PDA
    /// </summary>
    private bool ImplantUplink(EntityUid user, FixedPoint2 balance)
    {
        if (!_proto.Resolve<ListingPrototype>(FallbackUplinkCatalog, out var catalog))
            return false;

        if (!catalog.Cost.TryGetValue(TelecrystalCurrencyPrototype, out var cost))
            return false;

        if (balance < cost) // Can't use Math functions on FixedPoint2
            balance = 0;
        else
            balance = balance - cost;

        var implant = _subdermalImplant.AddImplant(user, FallbackUplinkImplant);

        if (!HasComp<StoreComponent>(implant))
            return false;

        SetUplink(user, implant.Value, balance);
        return true;
    }

    /// <summary>
    /// Finds the entity that can hold an uplink for a user.
    /// Usually this is a pda in their pda slot, but can also be in their hands. (but not pockets or inside bag, etc.)
    /// </summary>
    public EntityUid? FindUplinkTarget(EntityUid user)
    {
        return FindPdaUplinkTarget(user) ?? _goobUplink.FindPenUplinkTarget(user); // Goob - selfexplanatory
    }

    // Goob - pegged from FindUplinkTarget to FindPda
    public EntityUid? FindPdaUplinkTarget(EntityUid user)
    {
        // Try to find PDA in inventory
        if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.MoveNext(out var slot))
            {
                if (!slot.ContainedEntity.HasValue)
                    continue;

                if (HasComp<PdaComponent>(slot.ContainedEntity.Value))
                    return slot.ContainedEntity.Value;
            }
        }

        // Also check hands
        foreach (var item in _handsSystem.EnumerateHeld(user))
        {
            if (HasComp<PdaComponent>(item))
                return item;
        }

        return null;
    }
}
