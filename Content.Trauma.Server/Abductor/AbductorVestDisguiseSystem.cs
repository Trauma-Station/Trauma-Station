// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Server.Actions;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Content.Trauma.Shared.Body.Organ;

namespace Content.Trauma.Server.Abductor;

public sealed partial class AbductorVestDisguiseSystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private EntityQuery<VisualOrganMarkingsComponent> _organMarkingsQuery = default!;

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

        SubscribeLocalEvent<AbductorDisguiseStateComponent, DisguiseRevertEvent>(OnRevertEvent);
    }

    private void OnRevertEvent(Entity<AbductorDisguiseStateComponent> ent, ref DisguiseRevertEvent args)
    {
        args.Handled = true;
        RestoreAppearance(ent.AsNullable(), args.RaiseRenameEvents);
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

    public void ApplyDisguise(EntityUid user,
        HumanoidCharacterProfile? disguiseProfile = null,
        IEnumerable<Entity<VisualOrganComponent, VisualOrganMarkingsComponent>>? visualOrgans = null,
        EntProtoId? revertAction = null,
        bool allowRepeatedDisguise = false,
        bool raiseRenameEvents = true)
    {
        var name = Name(user);
        if (disguiseProfile?.Name == name)
            return;

        var disguise = EnsureComp<AbductorDisguiseStateComponent>(user);

        if (disguise.OriginalOrganData != null && !allowRepeatedDisguise)
            return;

        disguise.OriginalProfile ??= _humanoidProfile.CreateProfile(user);
        if (disguise.OriginalProfile is not { } ourProfile)
            return;

        ClearMarkings(user);
        disguise.OriginalName ??= name;
        var organData = CreateOrganData(visualOrgans);
        ApplyOrganData(organData, (user, disguise));

        disguiseProfile ??= HumanoidCharacterProfile.RandomWithSpecies("Human");
        disguiseProfile = disguiseProfile.WithKnowledge(ourProfile.Knowledge);
        _visualBody.ApplyProfileTo(user, disguiseProfile);
        _humanoidProfile.ApplyProfileTo(user, disguiseProfile);
        _metaData.SetEntityName(user, disguiseProfile.Name, raiseEvents: raiseRenameEvents);
        _identity.QueueIdentityUpdate(user);

        if (disguise.RevertAction != null || revertAction is not { } action)
            return;

        if (_actions.AddAction(user, ref disguise.RevertAction, action))
            _actions.SetEntityIcon(disguise.RevertAction.Value, user);
    }

    private void ApplyOrganData(Dictionary<Enum, DisguiseData> organData, Entity<AbductorDisguiseStateComponent> user)
    {
        // Don't overwrite data if it is not null already
        var isNull = user.Comp.OriginalOrganData == null;
        if (isNull)
            user.Comp.OriginalOrganData = new();
        foreach (var organ in GetOrgans(user.Owner))
        {
            if (!organData.TryGetValue(organ.Comp1.Layer, out var disguiseData))
                continue;

            if (isNull)
            {
                user.Comp.OriginalOrganData![organ.Owner] =
                    new DisguiseData(CloneData(organ.Comp1.Data),
                        organ.Comp1.SexStateOverrides,
                        organ.Comp2.MarkingData,
                        organ.Comp2.HideableLayers,
                        organ.Comp2.DependentHidingLayers);
            }

            organ.Comp1.Data = disguiseData.PrototypeLayerData;
            organ.Comp1.SexStateOverrides = disguiseData.SexStateOverrides;
            organ.Comp2.MarkingData = disguiseData.MarkingData;
            organ.Comp2.HideableLayers = disguiseData.HideableLayers;
            organ.Comp2.DependentHidingLayers = disguiseData.DependentHidingLayers;
            Dirty(organ);
        }
    }

    private Dictionary<Enum, DisguiseData> CreateOrganData(
        IEnumerable<Entity<VisualOrganComponent, VisualOrganMarkingsComponent>>? organs)
    {
        var organData = new Dictionary<Enum, DisguiseData>();

        if (organs is { } enumerable)
        {
            foreach (var organ in enumerable)
            {
                organData[organ.Comp1.Layer] =
                    new DisguiseData(CloneData(organ.Comp1.Data),
                        organ.Comp1.SexStateOverrides,
                        organ.Comp2.MarkingData,
                        organ.Comp2.HideableLayers,
                        organ.Comp2.DependentHidingLayers);
            }

            return organData;
        }

        foreach (var protoId in HumanVisualOrgans)
        {
            var entityProto = _prototype.Index<EntityPrototype>(protoId);
            if (!entityProto.TryGetComponent<VisualOrganComponent>(out var visualOrgan, Factory) ||
                !entityProto.TryGetComponent<VisualOrganMarkingsComponent>(out var markings, Factory))
                continue;

            organData[visualOrgan.Layer] =
                new DisguiseData(CloneData(visualOrgan.Data),
                    visualOrgan.SexStateOverrides,
                    markings.MarkingData,
                    markings.HideableLayers,
                    markings.DependentHidingLayers);
        }

        return organData;
    }

    public void RestoreAppearance(Entity<AbductorDisguiseStateComponent?, HumanoidProfileComponent?> user,
        bool raiseRenameEvents = true)
    {
        if (!Resolve(user, ref user.Comp1, ref user.Comp2, false) ||
            user.Comp1.OriginalOrganData is not { } organData ||
            user.Comp1.OriginalName is not { } name || user.Comp1.OriginalProfile is not { } profile)
            return;

        ClearMarkings(user);

        foreach (var organ in GetOrgans(user.Owner))
        {
            if (!organData.TryGetValue(organ.Owner, out var originalData))
                continue;

            organ.Comp1.Data = originalData.PrototypeLayerData;
            organ.Comp1.SexStateOverrides = originalData.SexStateOverrides;
            organ.Comp2.MarkingData = originalData.MarkingData;
            organ.Comp2.HideableLayers = originalData.HideableLayers;
            organ.Comp2.DependentHidingLayers = originalData.DependentHidingLayers;
            Dirty(organ);
        }

        profile = profile.WithKnowledge(user.Comp2.Knowledge);
        _visualBody.ApplyProfileTo(user.Owner, profile);
        _humanoidProfile.ApplyProfileTo(user.Owner, profile);
        _metaData.SetEntityName(user, name, raiseEvents: raiseRenameEvents);
        _identity.QueueIdentityUpdate(user);

        _actions.RemoveAction(user.Owner, user.Comp1.RevertAction);
        RemComp(user, user.Comp1);
    }

    // We have to clear markings before applying them because applying doesn't remove old markings
    private void ClearMarkings(EntityUid uid)
    {
        var ev = new ClearOrganMarkingsEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    // If we don't make a shallow clone of this, color will be overwritten on original object that PrototypeLayerData is referencing when changing profiles
    private PrototypeLayerData CloneData(PrototypeLayerData other)
    {
        return new PrototypeLayerData
        {
            Shader = other.Shader,
            TexturePath = other.TexturePath,
            RsiPath = other.RsiPath,
            State = other.State,
            Scale = other.Scale,
            Rotation = other.Rotation,
            Offset = other.Offset,
            Visible = other.Visible,
            Color = other.Color,
            MapKeys = other.MapKeys,
            RenderingStrategy = other.RenderingStrategy,
            CopyToShaderParameters = other.CopyToShaderParameters,
            Cycle = other.Cycle,
            Loop = other.Loop,
        };
    }

    public IEnumerable<Entity<VisualOrganComponent, VisualOrganMarkingsComponent>> GetOrgans(Entity<BodyComponent?> ent)
    {
        foreach (var (organ, visual) in _body.GetOrgans<VisualOrganComponent>(ent))
        {
            if (_organMarkingsQuery.TryComp(organ, out var markings))
                yield return (organ, visual, markings);
        }
    }
}
