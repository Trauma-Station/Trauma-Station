// SPDX-FileCopyrightText: 2025 August Eymann <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 Bandit <queenjess521@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 Ted Lukin <66275205+pheenty@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.TheManWhoSoldTheWorld;
using Content.Goobstation.Common.Weapons.Multishot;
using Content.Goobstation.Common.Weapons.NoWieldNeeded;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Smoking;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.HoloCigar;

/// <summary>
/// This is the system for the Holo-Cigar. - pure unadulterated shitcode below beware
/// </summary>
public sealed class HoloCigarSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _items = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private const string LitPrefix = "lit";
    private const string UnlitPrefix = "unlit";
    private const string MaskSlot = "mask";

    /// <inheritdoc/>o
    public override void Initialize()
    {
        SubscribeLocalEvent<HoloCigarComponent, GetVerbsEvent<AlternativeVerb>>(OnAddInteractVerb);

        SubscribeLocalEvent<HoloCigarAffectedGunComponent, DroppedEvent>(OnDroppedEvent);

        SubscribeLocalEvent<TheManWhoSoldTheWorldComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<TheManWhoSoldTheWorldComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<TheManWhoSoldTheWorldComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TheManWhoSoldTheWorldComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
    }

    private void OnAddInteractVerb(Entity<HoloCigarComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        AlternativeVerb verb = new()
        {
            Act = () => Toggle(ent),
            Message = Loc.GetString("holo-cigar-verb-desc"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/clock.svg.192dpi.png")),
            Text = Loc.GetString("holo-cigar-verb-text"),
        };

        args.Verbs.Add(verb);
    }

    #region Event Methods

    private void OnMobStateChangedEvent(Entity<TheManWhoSoldTheWorldComponent> ent, ref MobStateChangedEvent args)
    {
        if (!TryComp<HoloCigarComponent>(ent.Comp.HoloCigarEntity, out var holoCigarComponent))
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        _audio.Stop(holoCigarComponent.MusicEntity); // no music out of mouth duh

        _audio.PlayPredicted(ent.Comp.DeathAudio, ent, ent, AudioParams.Default.WithVolume(3f));
    }

    private void OnComponentShutdown(Entity<TheManWhoSoldTheWorldComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HoloCigarComponent>(ent.Comp.HoloCigarEntity, out var holoCigarComponent))
            return;

        _audio.Stop(holoCigarComponent.MusicEntity);
        ShutDownEnumerateRemoval(ent);

        if (!ent.Comp.AddedNoWieldNeeded)
            return;

        RemComp<NoWieldNeededComponent>(ent);
        ent.Comp.HoloCigarEntity = null;
    }

    private void ShutDownEnumerateRemoval(Entity<TheManWhoSoldTheWorldComponent> ent)
    {
        var query = EntityQueryEnumerator<HoloCigarAffectedGunComponent>();
        while (query.MoveNext(out var gun, out var comp))
        {
            if (comp.GunOwner != ent.Owner)
                continue;

            RestoreGun(gun);
        }
    }

    private void OnMapInitEvent(Entity<TheManWhoSoldTheWorldComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<NoWieldNeededComponent>(ent))
        {
            ent.Comp.AddedNoWieldNeeded = true;
            AddComp<NoWieldNeededComponent>(ent);
        }
        if (!_inventory.TryGetSlotEntity(ent, MaskSlot, out var cigarEntity) ||
            !HasComp<HoloCigarComponent>(cigarEntity))
            return;
        ent.Comp.HoloCigarEntity = cigarEntity;
    }

    private void OnDroppedEvent(Entity<HoloCigarAffectedGunComponent> ent, ref DroppedEvent args)
    {
        RestoreGun(ent);
    }

    private void OnPickupAttempt(Entity<TheManWhoSoldTheWorldComponent> ent, ref PickupAttemptEvent args)
    {
        if (!HasComp<GunComponent>(args.Item) || HasComp<HoloCigarAffectedGunComponent>(args.Item))
            return;

        var affected = EnsureComp<HoloCigarAffectedGunComponent>(args.Item);
        affected.GunOwner = ent.Owner;

        if (TryComp<MultishotComponent>(args.Item, out var multi))
        {
            affected.WasOriginallyMultishot = true;
            affected.OriginalMissChance = multi.MissChance;
            affected.OriginalSpreadModifier = multi.SpreadMultiplier;
            affected.OriginalSpreadAddition = multi.SpreadAddition;
        }

        multi = EnsureComp<MultishotComponent>(args.Item);
        multi.MissChance = 0f;
        multi.SpreadMultiplier = 1f; // no extra spread chuds
        multi.SpreadAddition = 0f;

        _gun.RefreshModifiers(args.Item);
    }

    private void Toggle(Entity<HoloCigarComponent> ent)
    {
        ent.Comp.Lit = !ent.Comp.Lit;
        Dirty(ent);

        var state = ent.Comp.Lit ? SmokableState.Lit : SmokableState.Unlit;
        var prefix = ent.Comp.Lit ? LitPrefix : UnlitPrefix;

        _appearance.SetData(ent, SmokingVisuals.Smoking, state);
        _clothing.SetEquippedPrefix(ent, prefix);
        _items.SetHeldPrefix(ent, prefix);

        if (!ent.Comp.Lit)
        {
            ent.Comp.MusicEntity = _audio.Stop(ent.Comp.MusicEntity);
            return;
        }

        // playing is not predicted as it spawns phantom audio for some reason
        if (_net.IsClient || _audio.PlayPvs(ent.Comp.Music, ent)?.Entity is not {} audio)
            return;

        EnsureComp<CopyrightedAudioComponent>(audio); // even a midi can cause copyright claims
        ent.Comp.MusicEntity = audio;
    }

    #endregion

    #region Helper Methods

    private void RestoreGun(EntityUid gun,
        HoloCigarAffectedGunComponent? cigarAffectedGunComponent = null,
        MultishotComponent? multiShotComp = null)
    {
        if (!Resolve(gun, ref cigarAffectedGunComponent, ref multiShotComp))
            return;

        switch (cigarAffectedGunComponent.WasOriginallyMultishot)
        {
            case false:
                RemComp<MultishotComponent>(gun);
                break;
            case true:
            {
                multiShotComp.MissChance = cigarAffectedGunComponent.OriginalMissChance;
                multiShotComp.SpreadMultiplier = cigarAffectedGunComponent.OriginalSpreadModifier;
                multiShotComp.SpreadAddition = cigarAffectedGunComponent.OriginalSpreadAddition;
                break;
            }
        }

        RemComp<HoloCigarAffectedGunComponent>(gun);
        _gun.RefreshModifiers(gun);
    }

    #endregion
}
