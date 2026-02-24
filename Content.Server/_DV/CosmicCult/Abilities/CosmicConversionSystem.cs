using Content.Goobstation.Common.Religion;
using Content.Server.Actions;
using Content.Server.Ghost;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.Light.Components;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.CosmicCult.Abilities;

public sealed class CosmicConversionSystem : EntitySystem
{
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private readonly SoundSpecifier _conversionSFX = new SoundPathSpecifier("/Audio/_DV/CosmicCult/conversion_start.ogg");
    private readonly SoundSpecifier _conversionEndSFX = new SoundPathSpecifier("/Audio/_DV/CosmicCult/conversion_end.ogg");
    private readonly EntProtoId _conversionVFX = "CosmicConversionAbilityVFX";
    private readonly EntProtoId _conversionEndVFX = "CosmicBlankAbilityVFX";
    private readonly EntProtoId _conversionDecal = "DecalSpawnerCosmicAsh";
    private readonly float _flickerRange = 8f;

    private readonly HashSet<Entity<PoweredLightComponent>> _lights = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicConversion>(OnCosmicConversion);
        SubscribeLocalEvent<CosmicCultComponent, CosmicConversionDoAfter>(OnDoAfter);
    }

    private void OnCosmicConversion(Entity<CosmicCultComponent> ent, ref EventCosmicConversion args)
    {
        var target = args.Target;

        if (!_mind.TryGetMind(target, out var mindId, out var mind)
        || HasComp<MindShieldComponent>(target)
        || HasComp<BibleUserComponent>(target))
            return;

        if (args.Handled)
            return;
        args.Handled = true;

        _actions.RemoveAction(ent.Owner, args.Action.Owner);
        _cult.UnlockInfluence(ent, "InfluenceConversion");

        Spawn(_conversionVFX, Transform(target).Coordinates);
        _audio.PlayPvs(_conversionSFX, ent);
        _cult.MalignEcho(ent);
        _stun.TryAddParalyzeDuration(target, ent.Comp.CosmicConversionDelay);

        _lights.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, _flickerRange, _lights, LookupFlags.StaticSundries);
        foreach (var light in _lights)
            _ghost.DoGhostBooEvent(light);

        var doargs = new DoAfterArgs(EntityManager, ent, ent.Comp.CosmicConversionDelay, new CosmicConversionDoAfter(), ent, target)
        {
            DistanceThreshold = 2.5f,
            Hidden = false,
            BreakOnHandChange = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = false,
        };
        _doAfter.TryStartDoAfter(doargs);
    }

    private void OnDoAfter(Entity<CosmicCultComponent> ent, ref CosmicConversionDoAfter args)
    {
        if (args.Args.Target is not { } target
            || args.Cancelled
            || args.Handled)
            return;
        args.Handled = true;

        _cultRule.CosmicConversion(ent, target);

        _audio.PlayPvs(_conversionEndSFX, ent);
        Spawn(_conversionEndVFX, Transform(target).Coordinates);
        Spawn(_conversionDecal, Transform(target).Coordinates);
    }
}
