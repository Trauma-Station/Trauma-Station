using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Goobstation.Shared.Disease.Components;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffect;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Goobstation.Shared.Disease.Systems;

public partial class SharedDiseaseSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    public const float MaxEffectSeverity = 1f; // magic numbers are EVIL and BAD

    protected virtual void InitializeEffects()
    {
        SubscribeLocalEvent<DiseaseAudioEffectComponent, DiseaseEffectEvent>(OnAudioEffect);
        SubscribeLocalEvent<DiseaseEmoteEffectComponent, DiseaseEffectEvent>(OnEmoteEffect);
        SubscribeLocalEvent<DiseaseSpreadEffectComponent, DiseaseEffectEvent>(OnDiseaseSpreadEffect);
        SubscribeLocalEvent<DiseaseForceSpreadEffectComponent, DiseaseEffectEvent>(OnDiseaseForceSpreadEffect);
        SubscribeLocalEvent<DiseaseFightImmunityEffectComponent, DiseaseEffectEvent>(OnFightImmunityEffect);
        SubscribeLocalEvent<DiseaseFlashEffectComponent, DiseaseEffectEvent>(OnFlashEffect);
        SubscribeLocalEvent<DiseasePopupEffectComponent, DiseaseEffectEvent>(OnPopupEffect);
        SubscribeLocalEvent<DiseasePryTileEffectComponent, DiseaseEffectEvent>(OnPryTileEffect);
        SubscribeLocalEvent<DiseaseEntityEffectComponent, DiseaseEffectEvent>(OnEntityEffect);
        SubscribeLocalEvent<DiseaseGrantComponentEffectComponent, DiseaseEffectEvent>(OnGrantComponentEffect);
        SubscribeLocalEvent<DiseaseGrantComponentEffectComponent, DiseaseEffectFailedEvent>(OnGrantComponentEffectFail);
    }

    private void OnGrantComponentEffect(Entity<DiseaseGrantComponentEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        EntityManager.AddComponents(args.Ent, ent.Comp.Components);
    }

    private void OnGrantComponentEffectFail(Entity<DiseaseGrantComponentEffectComponent> ent, ref DiseaseEffectFailedEvent args)
    {
        EntityManager.RemoveComponents(args.Ent, ent.Comp.Components);
    }

    private void OnAudioEffect(Entity<DiseaseAudioEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        if (_net.IsClient) // all clients predict disease updates...
            return;

        var sound = ent.Comp.Sound;
        if (ent.Comp.SoundFemale != null && TryComp<HumanoidProfileComponent>(args.Ent, out var humanoid) && humanoid.Sex == Sex.Female)
            sound = ent.Comp.SoundFemale;

        _audio.PlayPvs(sound, args.Ent);
    }

    private void OnEmoteEffect(Entity<DiseaseEmoteEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        if (ent.Comp.WithChat)
            _chat.TryEmoteWithChat(args.Ent, ent.Comp.Emote);
        else
            _chat.TryEmoteWithoutChat(args.Ent, ent.Comp.Emote);
    }

    private void OnDiseaseSpreadEffect(Entity<DiseaseSpreadEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        // for gear that makes you less(/more?) infective to others
        var ev = new DiseaseOutgoingSpreadAttemptEvent(
            ent.Comp.SpreadParams.Power,
            ent.Comp.SpreadParams.Chance,
            ent.Comp.SpreadParams.Type
        );
        RaiseLocalEvent(args.Ent, ref ev);

        if (ev.Power < 0 || ev.Chance < 0)
            return;

        var xform = Transform(args.Ent);
        var (selfPos, selfRot) = _transform.GetWorldPositionRotation(xform);

        var targets = _melee.ArcRayCast(selfPos, selfRot, ent.Comp.Arc, ent.Comp.Range, xform.MapID, args.Ent);

        foreach (var target in targets)
        {
            DoInfectionAttempt(target, args.Disease, ev.Power, ev.Chance * GetScale(args, ent.Comp), ent.Comp.SpreadParams.Type);
        }
    }

    private void OnDiseaseForceSpreadEffect(Entity<DiseaseForceSpreadEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        var transform = _transform.GetMapCoordinates(args.Ent);
        var targets = _lookup.GetEntitiesInRange<DamageableComponent>(transform, ent.Comp.Range);

        foreach (var target in targets)
        {
            if (!_random.Prob(ent.Comp.Chance * GetScale(args, ent.Comp)))
                continue;
            if (HasDisease(target.Owner, args.Disease.Comp.Genotype))
                continue;

            var newDisease = TryClone((args.Disease, args.Disease.Comp));
            if (newDisease == null)
                continue;

            MutateDisease(newDisease.Value);
            if (!TryInfect(target.Owner, newDisease.Value, true))
                QueueDel(newDisease);
            else if (ent.Comp.AddIcon)
                EnsureComp<StatusIconComponent>(target.Owner);
        }
    }

    private void OnFightImmunityEffect(Entity<DiseaseFightImmunityEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        ChangeImmunityProgress((args.Disease, args.Disease.Comp), ent.Comp.Amount * GetScale(args, ent.Comp));
    }

    private void OnFlashEffect(Entity<DiseaseFlashEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        if (_net.IsClient) // flashes twice if ran on both server and client
            return;

        var scale = GetScale(args, ent.Comp);
        var duration = ent.Comp.Duration * scale;
        _status.TryAddStatusEffect<FlashedComponent>(args.Ent, _flash.FlashedKey, duration, true);
        _movementMod.TryUpdateMovementSpeedModDuration(args.Ent, MovementModStatusSystem.FlashSlowdown, duration, ent.Comp.SlowTo);

        if (ent.Comp.StunDuration is {} stun)
            _stun.TryUpdateParalyzeDuration(args.Ent, stun * scale);
    }

    private void OnPopupEffect(Entity<DiseasePopupEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.HostOnly)
            _popup.PopupEntity(Loc.GetString(ent.Comp.String, ("source", args.Ent)), args.Ent, args.Ent, ent.Comp.Type);
        else
            _popup.PopupEntity(Loc.GetString(ent.Comp.String, ("source", args.Ent)), args.Ent, ent.Comp.Type);
    }

    private void OnPryTileEffect(Entity<DiseasePryTileEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        if (_net.IsClient)
            return;
        var xform = Transform(args.Ent);
        var mapPos = _transform.GetMapCoordinates(xform);
        if (!_mapMan.TryFindGridAt(mapPos, out var gridUid, out var grid))
            return;
        for (var i = 0; i < ent.Comp.Attempts; i++)
        {
            var distance = ent.Comp.Range * MathF.Sqrt(_random.NextFloat());
            var tileCoordinates = mapPos.Offset(_random.NextAngle().ToVec() * distance);
            var tile = _map.GetTileRef((gridUid, grid), tileCoordinates);
            if (_tile.DeconstructTile(tile))
                break;
        }
    }

    private void OnEntityEffect(Entity<DiseaseEntityEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        var scale = GetScale(args, ent);
        _effects.ApplyEffects(args.Ent, ent.Comp.Effects, scale);
    }

    protected float GetScale(DiseaseEffectEvent args, ScalingDiseaseEffect effect)
    {
        return (effect.SeverityScale ? args.Comp.Severity : 1f)
            * (effect.TimeScale ? (float)_updateInterval.TotalSeconds : 1f)
            * (effect.ProgressScale ? args.Disease.Comp.InfectionProgress : 1f);
    }

    private Entity<DiseaseEffectComponent>? RemoveRandomEffect(Entity<DiseaseComponent> ent, bool negativeOnly = false, bool allowFail = false)
    {
        // evil linq but how often is this gonna be called
        var effects = negativeOnly
            ? ent.Comp.Effects.ContainedEntities.Where(e => EffectQuery.TryComp(e, out var eff) && eff.Complexity > 0).ToList()
            : ent.Comp.Effects.ContainedEntities;

        if (effects.Count < 1)
        {
            if (!allowFail)
                Log.Error($"Disease {ToPrettyString(ent)} attempted to remove a random effect, but had either no or only positive effects left.");
            return null;
        }

        var index = _random.Next(effects.Count - 1);
        var effectUid = effects[index];
        TryRemoveEffect((ent, ent.Comp), effectUid);

        return EffectQuery.TryComp(effectUid, out var comp) ? (effectUid, comp) : null;
    }

    private Entity<DiseaseEffectComponent>? AddRandomEffect(Entity<DiseaseComponent> ent, bool negativeOnly = false)
    {
        if (!_proto.TryIndex(ent.Comp.AvailableEffects, out var effects))
        {
            Log.Error($"Disease {ToPrettyString(ent)} attempted to mutate to add an effect, but there are no valid effects for its type.");
            return null;
        }

        var weights = new Dictionary<string, float>(effects.Weights);
        if (negativeOnly)
            weights = weights.Where(w => _proto.Resolve<EntityPrototype>(w.Key, out var effProto)
                                        && effProto.TryGetComponent<DiseaseEffectComponent>(out _, Factory)
                                    ).ToDictionary(w => w.Key, w => w.Value);

        foreach (var diseaseEffect in ent.Comp.Effects.ContainedEntities) // no rolling effects we have
        {
            if (Prototype(diseaseEffect) is {} existing)
                weights.Remove(existing.ID);
        }

        if (weights.Count == 0)
        {
            Log.Warning($"Disease {ToPrettyString(ent)} attempted to mutate to add an effect, but it has all available effects.");
            return null;
        }

        var protoId = new EntProtoId(_random.Pick(weights));
        var proto = _proto.Index(protoId);
        Entity<DiseaseEffectComponent>? effect = null;
        if (proto.TryGetComponent<DiseaseEffectComponent>(out var effectComp, Factory))
            TryAdjustEffect((ent, ent.Comp), proto, out effect, _random.NextFloat(effectComp.MinSeverity, 1f));

        Dirty(ent);
        return effect;
    }

    #region public API

    /// <summary>
    /// Finds an effect of specified prototype, if any
    /// </summary>
    public bool FindEffect(Entity<DiseaseComponent?> ent, EntProtoId effectId, [NotNullWhen(true)] out Entity<DiseaseEffectComponent>? effect)
    {
        effect = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var effectProto = _proto.Index(effectId);
        foreach (var effectUid in ent.Comp.Effects.ContainedEntities)
        {
            if (effectProto != Prototype(effectUid))
                continue;

            if (!EffectQuery.TryComp(effectUid, out var diseaseEffect))
            {
                Log.Error($"Found disease effect {ToPrettyString(effectUid)} without DiseaseEffectComponent");
                return false;
            }

            effect = (effectUid, diseaseEffect);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the disease has an effect of specified prototype
    /// </summary>
    public bool HasEffect(Entity<DiseaseComponent?> ent, EntProtoId effectId)
        => FindEffect(ent, effectId, out _);

    /// <summary>
    /// Removes the specified disease effect from this disease
    /// </summary>
    public bool TryRemoveEffect(Entity<DiseaseComponent?> ent, EntityUid effect)
    {
        if (!Resolve(ent, ref ent.Comp) || !_container.Remove(effect, ent.Comp.Effects))
            return false;

        PredictedQueueDel(effect);
        return true;
    }

    /// <summary>
    /// Removes the disease effect of specified prototype from this disease
    /// </summary>
    public bool TryRemoveEffect(Entity<DiseaseComponent?> ent, EntProtoId effectId)
    {
        if (!Resolve(ent, ref ent.Comp) || !FindEffect(ent, effectId, out var effect))
            return false;

        return TryRemoveEffect(ent, effect.Value);
    }

    /// <summary>
    /// Adds the specified disease effect to this disease
    /// </summary>
    public bool TryAddEffect(Entity<DiseaseComponent?> ent, EntityUid effectUid, [NotNullWhen(true)] out Entity<DiseaseEffectComponent>? effect)
    {
        effect = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!EffectQuery.TryComp(effectUid, out var diseaseEffect))
        {
            Log.Error($"Tried to add disease effect {ToPrettyString(effect)}, but it had no DiseaseEffectComponent");
            return false;
        }
        effect = (effectUid, diseaseEffect);

        return _container.Insert(effectUid, ent.Comp.Effects);
    }

    /// <summary>
    /// Adds an effect of given prototype to the specified disease
    /// </summary>
    public bool TryAddEffect(Entity<DiseaseComponent?> ent, EntProtoId effectId, [NotNullWhen(true)] out Entity<DiseaseEffectComponent>? effect)
    {
        effect = null;
        if (!Resolve(ent, ref ent.Comp) || HasEffect(ent, effectId))
            return false;

        var effectUid = PredictedSpawnAtPosition(effectId, new EntityCoordinates(ent, Vector2.Zero));
        if (TryAddEffect(ent, effectUid, out effect))
            return true;

        PredictedDel(effectUid);
        return false;
    }

    /// <summary>
    /// Tries to adjust the strength of the effect of given prototype, creating or removing it as needed
    /// Non-present effects are assumed to have severity 0 regardless of the prototype's specified severity
    /// </summary>
    public bool TryAdjustEffect(Entity<DiseaseComponent?> ent, EntProtoId effectId, [NotNullWhen(true)] out Entity<DiseaseEffectComponent>? effect, float delta)
    {
        effect = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var spawned = false;
        if (!FindEffect(ent, effectId, out effect))
        {
            spawned = true;
            if (!TryAddEffect(ent, effectId, out effect))
                return false;
        }

        var e = effect.Value;
        if (spawned)
            e.Comp.Severity = 0f;

        e.Comp.Severity += delta;
        if (e.Comp.Severity <= 0f)
        {
            if (!TryRemoveEffect(ent, e))
                return false;
        }

        Dirty(e);
        Dirty(ent);
        return true;
    }

    #endregion
}
