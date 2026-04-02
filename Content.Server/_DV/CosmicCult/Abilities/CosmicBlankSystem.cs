using System.Collections.Immutable;
using Content.Server._DV.CosmicCult.Components;
using Content.Goobstation.Shared.Religion; // Goobstation - Bible
using Content.Server.Popups;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Components.Examine;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.CosmicCult.Abilities;

public sealed class CosmicBlankSystem : EntitySystem
{
    [Dependency] private readonly CosmicCultSystem _cult = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedCosmicCultSystem _cosmicCult = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DivineInterventionSystem _divineIntervention = default!;

    public SoundSpecifier BlankSFX = new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_blank.ogg");
    public EntProtoId BlankVFX = "CosmicBlankAbilityVFX";
    public EntProtoId SpawnWisp = "MobCosmicWisp";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlank>(OnCosmicBlank);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlankDoAfter>(OnCosmicBlankDoAfter);
    }

    private void OnCosmicBlank(Entity<CosmicCultComponent> uid, ref EventCosmicBlank args)
    {
        if (_cosmicCult.EntityIsCultist(args.Target)
            || HasComp<CosmicBlankComponent>(args.Target)
            || HasComp<ActiveNPCComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-generic-fail"), uid, uid);
            return;
        }

        if (_divineIntervention.TouchSpellDenied(args.Target))
            return;

        if (args.Handled)
            return;

        var doargs = new DoAfterArgs(EntityManager, uid, uid.Comp.CosmicBlankDelay, new EventCosmicBlankDoAfter(), uid, args.Target)
        {
            DistanceThreshold = 1.5f,
            Hidden = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };

        args.Handled = true;
        _doAfter.TryStartDoAfter(doargs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shuntQuery = EntityQueryEnumerator<InVoidComponent>();
        while (shuntQuery.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.ExitVoidTime
                || !_mind.TryGetMind(uid, out var mindEnt, out var mind))
                continue;

            mind.PreventGhosting = false;
            _mind.TransferTo(mindEnt, comp.OriginalBody);
            RemComp<CosmicBlankComponent>(comp.OriginalBody);
            RemComp<CosmicCultExamineComponent>(comp.OriginalBody);
            _popup.PopupEntity(Loc.GetString("cosmicability-blank-return"), comp.OriginalBody, comp.OriginalBody);
            QueueDel(uid);
        }
    }

    private void OnCosmicBlankDoAfter(Entity<CosmicCultComponent> uid, ref EventCosmicBlankDoAfter args)
    {
        if (args.Args.Target is not { } target
            || args.Cancelled
            || args.Handled)
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("cosmicability-blank-success",
            ("target", Identity.Entity(target, EntityManager))), uid, uid);

        _cult.MalignEcho(uid);

        ShuntTarget(target, uid.Comp.CosmicBlankDuration);
    }

    public void ShuntTarget(EntityUid target, TimeSpan duration)
    {
        var tgtpos = Transform(target).Coordinates;
        var spawnPoints = EntityManager
            .GetAllComponents(typeof(CosmicVoidSpawnComponent))
            .ToImmutableList();

        if (spawnPoints.IsEmpty)
            return;

        if (!TryComp<MindContainerComponent>(target, out var mindContainer)
            || mindContainer.Mind is not { } mindEnt)
            return;

        var mind = Comp<MindComponent>(mindEnt);
        mind.PreventGhosting = true;

        EnsureComp<CosmicBlankComponent>(target);
        var examine = EnsureComp<CosmicCultExamineComponent>(target);
        examine.CultistText = "cosmic-examine-text-abilityblank";

        _audio.PlayPvs(BlankSFX, target, AudioParams.Default.WithVolume(6f));
        Spawn(BlankVFX, tgtpos);
        var newSpawn = _random.Pick(spawnPoints);
        var spawnTgt = Transform(newSpawn.Uid).Coordinates;
        var mobUid = Spawn(SpawnWisp, spawnTgt);
        EnsureComp<InVoidComponent>(mobUid, out var inVoid);
        inVoid.OriginalBody = target;
        inVoid.ExitVoidTime = _timing.CurTime + duration;
        _mind.TransferTo(mindEnt, mobUid);
        _stun.TryKnockdown(target, duration + TimeSpan.FromSeconds(2), true);
        _popup.PopupEntity(Loc.GetString("cosmicability-blank-transfer"), mobUid, mobUid);
        _audio.PlayPvs(BlankSFX, spawnTgt, AudioParams.Default.WithVolume(6f));
        _color.RaiseEffect(Color.CadetBlue,
            [target],
            Filter.Pvs(target, entityManager: EntityManager));
        Spawn(BlankVFX, spawnTgt);
    }
}
