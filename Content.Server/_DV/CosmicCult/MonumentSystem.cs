using System.Linq;
using Content.Server._DV.CosmicCult.Components;
using Content.Server._DV.CosmicCult.EntitySystems;
using Content.Goobstation.Shared.Religion;
using Content.Goobstation.Shared.Religion.Nullrod; // Goobstation - Shitchap
using Content.Server.Actions;
using Content.Server.Atmos.Components;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Objectives.Components;
using Content.Shared._DV.CCVars;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Prototypes;
using Content.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Temperature.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

using Content.Medical.Common.Targeting; // Shitmed Change

namespace Content.Server._DV.CosmicCult;

public sealed class MonumentSystem : SharedMonumentSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly CosmicCorruptingSystem _corrupting = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cosmicRule = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    private static readonly EntProtoId CosmicGod = "MobCosmicGodSpawn";
    private static readonly EntProtoId MonumentCollider = "MonumentCollider";
    private EntityUid? _monumentStorageMap;

    public override void Update(float frameTime) // This Update() can fit so much functionality in it
    {
        base.Update(frameTime);

        var finaleQuery = EntityQueryEnumerator<CosmicFinaleComponent, MonumentComponent>(); // Enumerator for The Monument's Finale
        while (finaleQuery.MoveNext(out var uid, out var comp, out _))
        {
            if (comp.SongTimer is { } time
                && _timing.CurTime >= time)
            {
                comp.SongTimer = null;

                if (comp.SelectedSong is { } song)
                    _sound.DispatchStationEventMusic(uid, song, StationEventMusicType.CosmicCult);
            }

            if (comp.CurrentState == FinaleState.ActiveBuffer
                && _timing.CurTime >= comp.BufferTimer) // swap everything over when buffer timer runs out
            {
                comp.CurrentState = FinaleState.ActiveFinale;
                comp.FinaleTimer = _timing.CurTime + comp.FinaleRemainingTime;
                comp.SelectedSong = comp.FinaleMusic;
                _sound.StopStationEventMusic(uid, StationEventMusicType.CosmicCult);
                _appearance.SetData(uid, MonumentVisuals.FinaleReached, 3);
                _chatSystem.DispatchStationAnnouncement(uid,
                    Loc.GetString("cosmiccult-announce-finale-warning"),
                    null,
                    false,
                    null,
                    Color.FromHex("#cae8e8"));
                comp.SongTimer = _timing.CurTime + TimeSpan.FromSeconds(1);
            }
            else if (comp.CurrentState == FinaleState.ActiveFinale && _timing.CurTime >= comp.FinaleTimer) // trigger wincondition on time runout
            {
                var victoryQuery = EntityQueryEnumerator<CosmicVictoryConditionComponent>();

                while (victoryQuery.MoveNext(out _, out var victoryComp))
                    victoryComp.Victory = true;

                _sound.StopStationEventMusic(uid, StationEventMusicType.CosmicCult);
                Spawn(CosmicGod, Transform(uid).Coordinates);
                comp.CurrentState = FinaleState.Victory;
            }
        }

        var monumentQuery = EntityQueryEnumerator<MonumentComponent>();
        while (monumentQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.PhaseOutTimer is { } timer
                && _timing.CurTime >= timer)
            {
                OnMonumentPhaseOut((uid, comp));
                comp.PhaseOutTimer = null;
            }
        }

        /*var destinationQuery = EntityQueryEnumerator<MonumentMoveDestinationComponent>();
        while (destinationQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.PhaseInTimer is { } timer
                && _timing.CurTime >= timer)
            {
                OnMonumentPhaseIn((uid, comp));
                comp.PhaseInTimer = null;
            }
        }*/
    }

    private void OnMonumentPhaseOut(Entity<MonumentComponent> ent)
    {
        //todo check if anything gets messed up by doing this to the monument?
        _transform.SetParent(ent, EnsureStorageMapExists());
    }

    /*private void OnMonumentPhaseIn(Entity<MonumentMoveDestinationComponent> ent)
    {
        var colliderQuery = EntityQueryEnumerator<MonumentCollisionComponent>();

        while (colliderQuery.MoveNext(out var collider, out _))
            QueueDel(collider);

        if (ent.Comp.Monument is null)
            return;

        var xform = Transform(ent);
        _transform.SetCoordinates(ent.Comp.Monument.Value, xform.Coordinates);
        _transform.AnchorEntity(ent.Comp.Monument.Value); //no idea if this does anything but let's be safe about it
        Spawn(MonumentCollider, xform.Coordinates);

        if (TryComp<CosmicCorruptingComponent>(ent.Comp.Monument.Value, out var cosmicCorruptingComp))
            _corrupting.RecalculateStartingTiles((ent.Comp.Monument.Value, cosmicCorruptingComp));
    }*/

    private EntityUid EnsureStorageMapExists()
    {
        if (_monumentStorageMap != null
            && Exists(_monumentStorageMap))
            return _monumentStorageMap.Value;

        _monumentStorageMap = _map.CreateMap();
        _map.SetPaused(_monumentStorageMap.Value, true);
        return _monumentStorageMap.Value;
    }

    public void PhaseOutMonument(Entity<MonumentComponent> ent) =>
        ent.Comp.PhaseOutTimer = _timing.CurTime + TimeSpan.FromSeconds(0.45);

    public void UpdateMonumentAppearance(Entity<MonumentComponent> ent, bool tierUp) // this is kinda awful, but it works, and i've seen worse. improve it at thine leisure
    {
        if (_cosmicRule.AssociatedGamerule(ent) is not { } cult
            || !TryComp<CosmicFinaleComponent>(ent, out var finaleComp))
            return;

        _appearance.SetData(ent, MonumentVisuals.Monument, cult.Comp.CurrentTier);

        switch (cult.Comp.CurrentTier)
        {
            case 3:
                _appearance.SetData(ent, MonumentVisuals.Tier3, true);
                break;
            case 2:
                _appearance.SetData(ent, MonumentVisuals.Tier3, false);
                break;
        }

        if (tierUp)
        {
            var transformComp = EnsureComp<MonumentTransformingComponent>(ent);
            transformComp.EndTime = _timing.CurTime + ent.Comp.TransformTime;
            _appearance.SetData(ent, MonumentVisuals.Transforming, true);
        }

        if (finaleComp.CurrentState != FinaleState.Unavailable)
            _appearance.SetData(ent, MonumentVisuals.FinaleReached, true);
    }

    public void MonumentTier1(Entity<MonumentComponent> uid)
    {
        if (_cosmicRule.AssociatedGamerule(uid) is not { } cult)
            return;

        UpdateMonumentAppearance(uid, false);

        //basically completely unnecessary, but putting this here for sanity & futureproofing - ruddygreat
        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var cultComp))
        {
            foreach (var influenceProto in _protoMan
                .EnumeratePrototypes<InfluencePrototype>()
                .Where(influenceProto => influenceProto.Tier == 1))
                cultComp.UnlockedInfluences.Add(influenceProto.ID);

            Dirty(cultist, cultComp);
        }

        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();

        while (objectiveQuery.MoveNext(out _, out var objectiveComp))
            objectiveComp.Tier = 1;
    }

    public void MonumentTier2(Entity<MonumentComponent> uid)
    {
        if (_cosmicRule.AssociatedGamerule(uid) is not { } cult)
            return;

        UpdateMonumentAppearance(uid, true);

        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();

        while (objectiveQuery.MoveNext(out _, out var objectiveComp))
            objectiveComp.Tier = 2;

        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var cultComp))
        {
            foreach (var influenceProto in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == 2))
                cultComp.UnlockedInfluences.Add(influenceProto.ID);

            cultComp.EntropyBudget += cult.Comp.TotalCrew / 100 * 10; // pity system. 10% of the playercount worth of entropy on tier up

            Dirty(cultist, cultComp);
        }

        Dirty(uid);
    }

    public void MonumentTier3(Entity<MonumentComponent> uid)
    {
        if (_cosmicRule.AssociatedGamerule(uid) is not { } cult)
            return;

        UpdateMonumentAppearance(uid, true);

        var objectiveQuery = EntityQueryEnumerator<CosmicTierConditionComponent>();
        while (objectiveQuery.MoveNext(out var _, out var objectiveComp))
            objectiveComp.Tier = 3;

        var query = EntityQueryEnumerator<CosmicCultComponent>();
        while (query.MoveNext(out var cultist, out var cultComp))
        {
            EnsureComp<PressureImmunityComponent>(cultist);
            EnsureComp<TemperatureImmunityComponent>(cultist);

            var ev = new UnholyStatusChangedEvent(cultist, cultist, true);
            RaiseLocalEvent(cultist, ref ev);

            foreach (var influenceProto in _protoMan.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == 3))
                cultComp.UnlockedInfluences.Add(influenceProto.ID);

            cultComp.EntropyBudget += cult.Comp.TotalCrew / 100 * 10; //pity system. 10% of the playercount worth of entropy on tier up
            Dirty(cultist, cultComp);
        }
        Dirty(uid);
    }

    public void ReadyFinale(Entity<MonumentComponent> uid, CosmicFinaleComponent finaleComp)
    {
        if (TryComp<CosmicCorruptingComponent>(uid, out var comp))
            _corrupting.Enable((uid, comp));

        if (TryComp<ActivatableUIComponent>(uid, out var uiComp))
        {
            uiComp.Key = null; //kazne called this the laziest way to disable a UI ever
        }

        finaleComp.CurrentState = FinaleState.ReadyBuffer;

        _popup.PopupCoordinates(Loc.GetString("cosmiccult-finale-ready"), Transform(uid).Coordinates, PopupType.Large);
    }
}
