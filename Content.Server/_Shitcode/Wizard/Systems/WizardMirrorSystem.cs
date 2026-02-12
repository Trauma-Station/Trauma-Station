// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Guardian;
using Content.Server.Mind;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared._Goobstation.Wizard.BindSoul;
using Content.Shared._Goobstation.Wizard.MagicMirror;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Wizard.Systems;

public sealed class WizardMirrorSystem : SharedWizardMirrorSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<WizardMirrorComponent>(WizardMirrorUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUiClosed);
            subs.Event<WizardMirrorMessage>(OnMessage);
        });
    }

    private void OnMessage(Entity<WizardMirrorComponent> ent, ref WizardMirrorMessage args)
    {
        Log.Debug($"Mirror message for {ToPrettyString(ent.Comp.Target)}");
        if (!TryComp(ent.Comp.Target, out HumanoidProfileComponent? humanoid))
            return;

        ForceLoadProfile(ent.Comp.Target.Value, ent.Comp, args.Profile, humanoid);
    }

    private void OnUiClosed(Entity<WizardMirrorComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
        Dirty(ent);
    }

    private void ForceLoadProfile(EntityUid target,
        WizardMirrorComponent component,
        HumanoidCharacterProfile profile,
        HumanoidProfileComponent humanoid)
    {
        var age = humanoid.Age;
        if (humanoid.Species != profile.Species && component.AllowedSpecies.Contains(profile.Species) &&
            _proto.TryIndex(profile.Species, out var speciesProto))
        {
            if (HasComp<GuardianHostComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("wizard-mirror-guardian-change-species-fail"), target, target);
                return;
            }

            var config = new PolymorphConfiguration
            {
                Entity = speciesProto.Prototype,
                TransferName = true,
                TransferDamage = true,
                Forced = true,
                Inventory = PolymorphInventoryChange.Transfer,
                RevertOnCrit = false,
                RevertOnDeath = false,
                ComponentsToTransfer = new()
                {
                    new("LanguageKnowledge"),
                    new("LanguageSpeaker"),
                    new("Grammar"),
                    new("Wizard", mirror: true),
                    new("Apprentice", mirror: true),
                    new("UniversalLanguageSpeaker", mirror: true),
                    new("TowerOfBabel", mirror: true),
                    new("CanEnchant", mirror: true),
                    new("CanPerformCombo"),
                    new("MartialArtsKnowledge"),
                    new("NinjutsuSneakAttack"),
                    new("NpcFactionMember"),
                },
            };
            if (_polymorph.PolymorphEntity(target, config) is {} newUid)
            {
                RemCompDeferred<PolymorphedEntityComponent>(newUid);
                humanoid = EnsureComp<HumanoidProfileComponent>(newUid);
                target = newUid;
            }
        }

        _meta.SetEntityName(target, profile.Name);
        Humanoid.ApplyProfileTo((target, humanoid), profile);
        _visualBody.ApplyProfileTo(target, profile);

        if (_mind.TryGetMind(target, out var mind, out _) && TryComp(mind, out SoulBoundComponent? soulBound))
        {
            soulBound.Name = profile.Name;
            soulBound.Age = age;
            soulBound.Gender = profile.Gender;
            soulBound.Sex = profile.Sex;
            Dirty(mind, soulBound);
        }
    }
}
