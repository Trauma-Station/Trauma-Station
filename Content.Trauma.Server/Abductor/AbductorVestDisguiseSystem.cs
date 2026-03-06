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

        SubscribeLocalEvent<AbductorVestDisguiseComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AbductorVestDisguiseComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<AbductorVestDisguiseComponent, ItemSwitchedEvent>(OnItemSwitch);
    }

    private void OnEquipped(Entity<AbductorVestDisguiseComponent> ent, ref GotEquippedEvent args)
    {
        if (!TryComp<AbductorVestComponent>(ent, out var vest) || vest.CurrentState != AbductorArmorModeType.Stealth)
            return;

        ApplyDisguise(ent, args.Equipee);
    }

    private void OnUnequipped(Entity<AbductorVestDisguiseComponent> ent, ref GotUnequippedEvent args)
    {
        RestoreAppearance(ent, args.Equipee);
    }

    private void OnItemSwitch(Entity<AbductorVestDisguiseComponent> ent, ref ItemSwitchedEvent args)
    {
        var user = Transform(ent).ParentUid;
        if (!HasComp<MobStateComponent>(user))
            return;

        if (Enum.TryParse<AbductorArmorModeType>(args.State, ignoreCase: true, out var state))
        {
            if (state == AbductorArmorModeType.Stealth)
                ApplyDisguise(ent, user);
            else
                RestoreAppearance(ent, user);
        }
    }

    private void ApplyDisguise(Entity<AbductorVestDisguiseComponent> ent, EntityUid user)
    {
        if (!TryComp<BodyComponent>(user, out var body) || body.Organs == null)
            return;

        if (ent.Comp.OriginalOrganData != null)
            return;

        ent.Comp.OriginalName = MetaData(user).EntityName;
        ent.Comp.OriginalOrganData = new();

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

            ent.Comp.OriginalOrganData[organUid] = visualOrgan.Data;
            visualOrgan.Data = humanData;
            Dirty(organUid, visualOrgan);
        }

        var disguiseProfile = HumanoidCharacterProfile.RandomWithSpecies("Human");
        _visualBody.ApplyProfileTo(user, disguiseProfile);
        _humanoidProfile.ApplyProfileTo(user, disguiseProfile);
        _metaData.SetEntityName(user, disguiseProfile.Name);
        _identity.QueueIdentityUpdate(user);
    }

    private void RestoreAppearance(Entity<AbductorVestDisguiseComponent> ent, EntityUid user)
    {
        if (ent.Comp.OriginalOrganData == null || ent.Comp.OriginalName == null)
            return;

        if (!TryComp<BodyComponent>(user, out var body) || body.Organs == null)
            return;

        foreach (var organUid in body.Organs.ContainedEntities)
        {
            if (!TryComp<VisualOrganComponent>(organUid, out var visualOrgan))
                continue;

            if (!ent.Comp.OriginalOrganData.TryGetValue(organUid, out var originalData))
                continue;

            visualOrgan.Data = originalData;
            Dirty(organUid, visualOrgan);
        }

        _metaData.SetEntityName(user, ent.Comp.OriginalName);
        _identity.QueueIdentityUpdate(user);

        ent.Comp.OriginalOrganData = null;
        ent.Comp.OriginalName = null;
    }
}
