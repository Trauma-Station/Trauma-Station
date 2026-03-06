// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Medical.Shared.ItemSwitch;
using Content.Server.Humanoid.Systems;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Abductor;

public sealed class AbductorVestDisguiseSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

    private static readonly List<EntProtoId> HumanVisualOrgans = new()
    {
        "OrganHumanTorso",
        "OrganHumanHead",
        "OrganHumanArmLeft",
        "OrganHumanArmRight",
        "OrganHumanHandLeft",
        "OrganHumanHandRight",
        "OrganHumanLegLeft",
        "OrganHumanLegRight",
        "OrganHumanFootLeft",
        "OrganHumanFootRight",
        "OrganHumanEyes",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbductorVestDisguiseComponent, ComponentInit>(OnDisguiseAdded);
        SubscribeLocalEvent<AbductorVestDisguiseComponent, ComponentShutdown>(OnDisguiseRemoved);
    }

    private void OnDisguiseAdded(Entity<AbductorVestDisguiseComponent> ent, ref ComponentInit args)
    {
        var user = Transform(ent).ParentUid;
        if (!HasComp<MobStateComponent>(user))
            return;

        ApplyDisguise(user);
    }

    private void OnDisguiseRemoved(Entity<AbductorVestDisguiseComponent> ent, ref ComponentShutdown args)
    {
        var user = Transform(ent).ParentUid;
        if (!HasComp<MobStateComponent>(user))
            return;

        RestoreAppearance(user);
    }

    private void ApplyDisguise(EntityUid user)
    {
        if (!TryComp<BodyComponent>(user, out var body) || body.Organs == null)
            return;

        var disguise = EnsureComp<AbductorDisguiseStateComponent>(user);

        if (disguise.OriginalOrganData != null)
            return;

        disguise.OriginalName = MetaData(user).EntityName;
        disguise.OriginalOrganData = new();

        var humanOrganData = new Dictionary<Enum, PrototypeLayerData>();
        foreach (var proto in HumanVisualOrgans)
        {
            var organEnt = Spawn(proto, Transform(user).Coordinates);
            if (TryComp<VisualOrganComponent>(organEnt, out var visualOrgan))
                humanOrganData[visualOrgan.Layer] = visualOrgan.Data;
            QueueDel(organEnt);
        }

        foreach (var organUid in body.Organs.ContainedEntities)
        {
            if (!TryComp<VisualOrganComponent>(organUid, out var visualOrgan))
                continue;

            if (!humanOrganData.TryGetValue(visualOrgan.Layer, out var humanData))
                continue;

            disguise.OriginalOrganData[organUid] = visualOrgan.Data;
            visualOrgan.Data = humanData;
            Dirty(organUid, visualOrgan);
        }

        var disguiseProfile = HumanoidCharacterProfile.RandomWithSpecies("Human");
        _visualBody.ApplyProfileTo(user, disguiseProfile);
        _humanoidProfile.ApplyProfileTo(user, disguiseProfile);
        _metaData.SetEntityName(user, disguiseProfile.Name);
        _identity.QueueIdentityUpdate(user);
    }

    private void RestoreAppearance(EntityUid user)
    {
        if (!TryComp<AbductorDisguiseStateComponent>(user, out var disguise))
            return;

        if (disguise.OriginalOrganData == null || disguise.OriginalName == null)
            return;

        if (!TryComp<BodyComponent>(user, out var body) || body.Organs == null)
            return;

        foreach (var organUid in body.Organs.ContainedEntities)
        {
            if (!TryComp<VisualOrganComponent>(organUid, out var visualOrgan))
                continue;

            if (!disguise.OriginalOrganData.TryGetValue(organUid, out var originalData))
                continue;

            visualOrgan.Data = originalData;
            Dirty(organUid, visualOrgan);
        }

        _metaData.SetEntityName(user, disguise.OriginalName);
        _identity.QueueIdentityUpdate(user);

        RemComp<AbductorDisguiseStateComponent>(user);
    }
}
