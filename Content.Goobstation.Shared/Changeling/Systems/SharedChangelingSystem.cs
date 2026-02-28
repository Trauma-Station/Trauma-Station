// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Spatison <137375981+Spatison@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Changeling.Components;
using Content.Goobstation.Shared.Overlays;
using Content.Trauma.Common.Kitchen;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract partial class SharedChangelingSystem : EntitySystem
{
    [Dependency] protected readonly BodySystem Body = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public static readonly EntProtoId FakeArmbladePrototype = "FakeArmBladeChangeling";
    public static readonly EntProtoId DartGunPrototype = "DartGunChangeling";

    public static readonly EntProtoId BoneShardPrototype = "ThrowingStarChangeling";

    public static readonly EntProtoId ArmorPrototype = "ChangelingClothingOuterArmor";
    public static readonly EntProtoId ArmorHelmetPrototype = "ChangelingClothingHeadHelmet";

    public override void Initialize()
    {
        base.Initialize();

        InitAbilities();

        SubscribeLocalEvent<ChangelingIdentityComponent, SwitchableOverlayToggledEvent>(OnVisionToggle);
        SubscribeLocalEvent<ChangelingIdentityComponent, ButcherAttemptEvent>(OnButcherAttempt);
        SubscribeLocalEvent<AbsorbedComponent, ButcherAttemptEvent>(OnButcherAttempt);
    }

    private void OnVisionToggle(Entity<ChangelingIdentityComponent> ent, ref SwitchableOverlayToggledEvent args)
    {
        if (args.User != ent.Owner)
            return;

        if (TryComp(ent, out EyeProtectionComponent? eyeProtection))
            eyeProtection.ProtectionTime = args.Activated ? TimeSpan.Zero : TimeSpan.FromSeconds(10);

        UpdateFlashImmunity(ent, !args.Activated);
    }

    protected virtual void UpdateFlashImmunity(EntityUid uid, bool active) { }

    private void OnButcherAttempt(EntityUid uid, IComponent comp, ref ButcherAttemptEvent args)
    {
        // intentionally using the same popup for both components so you have to use 1% of your brain
        args.CancelPopup = "butcherable-deny-absorbed";
    }

    #region Helper methods

    public void PlayMeatySound(EntityUid uid, ChangelingIdentityComponent comp, bool predicted = false)
    {
        // TODO: make this a fucking sound collection...
        var sound = _random.Pick(comp.SoundPool);
        var param = AudioParams.Default.WithVolume(-3f);
        if (predicted)
            Audio.PlayPredicted(sound, uid, uid, param);
        else
            Audio.PlayPvs(sound, uid, param);
    }

    public bool TryToggleItem(EntityUid uid, EntProtoId proto, ChangelingIdentityComponent comp, out EntityUid? equipment)
    {
        equipment = null;
        if (comp.Equipment.TryGetValue(proto.Id, out var netItem))
        {
            PredictedQueueDel(GetEntity(netItem));
            // assuming that it exists
            comp.Equipment.Remove(proto.Id);
            Dirty(uid, comp);
            return true;
        }

        var item = PredictedSpawnAtPosition(proto, Transform(uid).Coordinates);
        if (!Hands.TryForcePickupAnyHand(uid, item))
        {
            Popup.PopupEntity(Loc.GetString("changeling-fail-hands"), uid, uid);
            PredictedDel(item);
            return false;
        }

        comp.Equipment.Add(proto.Id, GetNetEntity(item));
        Dirty(uid, comp);
        equipment = item;
        return true;
    }

    public bool TryToggleArmor(EntityUid uid, ChangelingIdentityComponent comp, (EntProtoId, string)[] armors)
    {
        if (comp.ActiveArmor is {} active)
        {
            // Unequip armor
            foreach (var armor in active)
                PredictedQueueDel(GetEntity(armor));

            Audio.PlayPredicted(comp.ArmourStripSound, uid, uid);

            comp.ActiveArmor = null!;
            comp.ChemicalRegenMultiplier += 0.25f; // chem regen debuff removed
            Dirty(uid, comp);
            return true;
        }

        // Equip armor
        var newArmor = new List<NetEntity>();
        var coords = Transform(uid).Coordinates;
        foreach (var (proto, slot) in armors)
        {
            var armor = PredictedSpawnAtPosition(proto, coords);
            if (!_inventory.TryEquip(uid, armor, slot, force: true))
            {
                PredictedDel(armor);
                foreach (var delArmor in newArmor)
                    PredictedDel(GetEntity(delArmor));

                return false;
            }
            newArmor.Add(GetNetEntity(armor));
        }

        Audio.PlayPredicted(comp.ArmourSound, uid, uid);

        comp.ActiveArmor = newArmor;
        comp.ChemicalRegenMultiplier -= 0.25f; // base chem regen slowed by a flat 25%
        Dirty(uid, comp);
        return true;
    }

    #endregion
}
