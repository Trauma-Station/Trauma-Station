// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Bitrunning.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Trauma.Server.Bitrunning.Components;
using Content.Trauma.Shared.Bitrunning;
using Content.Trauma.Shared.Bitrunning.Components;
using Content.Shared.Destructible;
using Content.Shared.Humanoid;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Bitrunning.Systems;

public sealed partial class NetpodSystem : EntitySystem
{
    [Dependency] private QuantumServerSystem _server = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ILocalizationManager _loc = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IGameTiming _timing = default!;

    private Dictionary<ProtoId<StartingGearPrototype>, string>? _startingGearToJobName;

    private static readonly TimeSpan PodAnimationDuration = TimeSpan.FromSeconds(1.3);
    private static readonly TimeSpan StateValidationInterval = TimeSpan.FromSeconds(5);
    private TimeSpan _nextValidationTime;
    private const string ServerSinkPort = "BitrunningNetpodSink";
    private readonly HashSet<EntityUid> _ejectingPods = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<NetpodComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NetpodComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NetpodComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<NetpodComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<NetpodComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<NetpodComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<NetpodComponent, ContainerIsRemovingAttemptEvent>(OnOccupantRemoveAttempt);
        SubscribeLocalEvent<NetpodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<NetpodComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<NetpodComponent, NetpodSelectLoadoutMessage>(OnSelectLoadout);
        SubscribeLocalEvent<NetpodComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<NetpodComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextValidationTime)
            return;

        _nextValidationTime = _timing.CurTime + StateValidationInterval;

        var query = EntityQueryEnumerator<NetpodComponent, NetpodContainerComponent>();
        while (query.MoveNext(out var uid, out var pod, out var container))
        {
            var dirty = false;

            var contained = container.BodyContainer.ContainedEntity;
            if (pod.Occupant != contained)
            {
                pod.Occupant = contained;
                dirty = true;
            }

            if (pod.Avatar is { } avatar && !Exists(avatar))
            {
                pod.Avatar = null;
                dirty = true;
            }

            if (pod.LinkedServer is { } server && (!Exists(server) || !HasComp<QuantumServerComponent>(server)))
            {
                pod.LinkedServer = null;
                dirty = true;
            }

            if (!dirty)
                continue;

            UpdateVisuals((uid, pod));
        }
    }

    private void OnInit(Entity<NetpodComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ServerSinkPort);
        var containerComp = EnsureComp<NetpodContainerComponent>(ent);
        containerComp.BodyContainer = _container.EnsureContainer<ContainerSlot>(ent, "netpod-body");
        ent.Comp.Occupant = containerComp.BodyContainer.ContainedEntity;
        UpdateVisuals(ent);
    }

    private void OnMapInit(Entity<NetpodComponent> ent, ref MapInitEvent args)
    {
        RefreshLinkedServer(ent);
    }

    private void OnDestroyed(Entity<NetpodComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Avatar != null)
            _server.DisconnectAvatar(ent.Comp.Avatar.Value, true);

        EjectOccupant(ent.Owner);
    }

    private void OnTerminating(Entity<NetpodComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Avatar != null)
            _server.DisconnectAvatar(ent.Comp.Avatar.Value, true);
    }

    private void OnPowerChanged(Entity<NetpodComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        if (ent.Comp.Avatar != null)
        {
            _server.DisconnectAvatar(ent.Comp.Avatar.Value, true);
        }

        Timer.Spawn(TimeSpan.Zero,
            () =>
        {
            if (Exists(ent.Owner))
                EjectOccupant(ent.Owner);
        });
        UpdateVisuals(ent);
    }

    private void OnEntInserted(Entity<NetpodComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != "netpod-body")
            return;

        if (args.Entity == EntityUid.Invalid || !Exists(args.Entity))
            return;

        if (TryComp<ApcPowerReceiverComponent>(ent.Owner, out var power) && !power.Powered)
        {
            Timer.Spawn(TimeSpan.Zero,
                () =>
            {
                if (Exists(ent.Owner))
                    EjectOccupant(ent.Owner);
            });
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-no-power"), ent, args.Entity);
            return;
        }

        if (!HasComp<HumanoidProfileComponent>(args.Entity))
        {
            Timer.Spawn(TimeSpan.Zero,
                () =>
            {
                if (Exists(ent.Owner))
                    EjectOccupant(ent.Owner);
            });
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-enter-failed"), ent, args.Entity);
            return;
        }

        if (TryComp<MobStateComponent>(args.Entity, out var mobState) && mobState.CurrentState == MobState.Dead)
        {
            Timer.Spawn(TimeSpan.Zero,
                () =>
            {
                if (Exists(ent.Owner))
                    EjectOccupant(ent.Owner);
            });
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-enter-failed"), ent, args.Entity);
            return;
        }

        ent.Comp.Occupant = args.Entity;
        SetVisualState(ent, NetpodVisualState.Closing);
        _audio.PlayPvs(ent.Comp.CloseSound, ent);
        AutoConnectDelay(ent.Owner, args.Entity, ent.Comp.AutoConnectDelay);
        Timer.Spawn(PodAnimationDuration,
            () =>
        {
            if (!Exists(ent.Owner))
                return;

            UpdateVisuals(ent);
        });
    }

    private void OnEntRemoved(Entity<NetpodComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != "netpod-body")
            return;

        ent.Comp.Occupant = null;
        SetVisualState(ent, NetpodVisualState.Opening);
        _audio.PlayPvs(ent.Comp.OpenSound, ent);
        Timer.Spawn(PodAnimationDuration,
            () =>
        {
            if (!Exists(ent.Owner))
                return;

            UpdateVisuals(ent);
        });
    }

    private void OnOccupantRemoveAttempt(Entity<NetpodComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (args.Container.ID != "netpod-body")
            return;

        if (_ejectingPods.Contains(ent.Owner))
            return;

        if (ent.Comp.Avatar is not { } avatar)
            return;

        if (Exists(avatar))
            _server.DisconnectAvatar(avatar, true);

        ent.Comp.Occupant = TryComp<NetpodContainerComponent>(ent.Owner, out var containerComp)
            ? containerComp.BodyContainer.ContainedEntity
            : null;
        UpdateVisuals(ent);
    }

    private void OnUiOpened(Entity<NetpodComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent, args.Actor);
    }

    private void OnSelectLoadout(Entity<NetpodComponent> ent, ref NetpodSelectLoadoutMessage args)
    {
        if (!_prototype.HasIndex<StartingGearPrototype>(args.LoadoutId))
            return;

        if (!TryComp<NetpodLoadoutComponent>(ent.Owner, out var loadout))
            return;

        HashSet<ProtoId<StartingGearPrototype>> allowed = GetAllowedLoadouts(loadout, args.Actor);

        if (!allowed.Contains(args.LoadoutId))
            return;

        loadout.PreferredLoadout = args.LoadoutId;
        Dirty(ent.Owner, loadout);
        UpdateUi(ent, args.Actor);
    }

    private HashSet<ProtoId<StartingGearPrototype>> GetAllowedLoadouts(NetpodLoadoutComponent loadout, EntityUid user)
    {
        if (HasCompInContainerTree<BitrunningProfileDiskComponent>(user) && TryComp<HumanoidProfileComponent>(user, out var humanoid) && loadout.AllowedLoadoutSpecie.TryGetValue(humanoid.Species, out var speciesLoadouts))
            return new HashSet<ProtoId<StartingGearPrototype>>(speciesLoadouts);

        return new HashSet<ProtoId<StartingGearPrototype>>(loadout.AllowedLoadout);
    }

    public ProtoId<StartingGearPrototype>? GetResolvedPreferredLoadout(NetpodLoadoutComponent loadout, EntityUid user)
    {
        if (HasCompInContainerTree<BitrunningProfileDiskComponent>(user) && TryComp<HumanoidProfileComponent>(user, out var humanoid))
        {
            if (loadout.PreferredLoadout != null && loadout.AllowedLoadoutSpecie.TryGetValue(humanoid.Species, out var allowed) && allowed.Contains(loadout.PreferredLoadout.Value))
                return loadout.PreferredLoadout;

            if (loadout.PreferredLoadoutSpecie.TryGetValue(humanoid.Species, out var speciePreferred))
                return speciePreferred;
        }

        return loadout.PreferredLoadout;
    }

    private bool HasCompInContainerTree<T>(EntityUid root) where T : Component
    {
        var queue = new Queue<EntityUid>();
        var visited = new HashSet<EntityUid>();
        queue.Enqueue(root);

        while (queue.TryDequeue(out var current))
        {
            if (!visited.Add(current))
                continue;

            if (HasComp<T>(current))
                return true;

            if (!TryComp<ContainerManagerComponent>(current, out var manager))
                continue;

            foreach (var container in manager.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    queue.Enqueue(contained);
                }
            }
        }

        return false;
    }

    private void UpdateUi(Entity<NetpodComponent> ent, EntityUid user)
    {
        _startingGearToJobName ??= BuildStartingGearLookup();
        var loadouts = new List<NetpodLoadoutEntry>();

        if (TryComp<NetpodLoadoutComponent>(ent.Owner, out var loadoutComp))
        {
            HashSet<ProtoId<StartingGearPrototype>> allowed = GetAllowedLoadouts(loadoutComp, user);

            foreach (ProtoId<StartingGearPrototype> loadoutId in allowed)
            {
                if (!_prototype.TryIndex(loadoutId, out _))
                    continue;

                loadouts.Add(new NetpodLoadoutEntry(loadoutId, GetLoadoutDisplayName(loadoutId)));
            }

            loadouts.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            _ui.SetUiState(ent.Owner, NetpodUiKey.Key, new NetpodBoundUiState(GetResolvedPreferredLoadout(loadoutComp, user), loadouts));
        }
        else
        {
            _ui.SetUiState(ent.Owner, NetpodUiKey.Key, new NetpodBoundUiState(null, loadouts));
        }
    }

    public bool EjectOccupant(EntityUid podUid)
    {
        if (!TryComp<NetpodComponent>(podUid, out _) ||
            !TryComp<NetpodContainerComponent>(podUid, out var containerComp))
            return false;

        if (containerComp.BodyContainer.ContainedEntity is not { } contained)
            return false;

        _ejectingPods.Add(podUid);
        try
        {
            return _container.Remove(contained, containerComp.BodyContainer);
        }
        finally
        {
            _ejectingPods.Remove(podUid);
        }
    }

    private EntityUid? ResolveServer(Entity<NetpodComponent> ent)
    {
        if (ent.Comp.LinkedServer is { } linked && Exists(linked) && HasComp<QuantumServerComponent>(linked))
            return linked;

        RefreshLinkedServer(ent);
        return ent.Comp.LinkedServer;
    }

    private void OnNewLink(Entity<NetpodComponent> ent, ref NewLinkEvent args)
    {
        if (args.Sink != ent.Owner || args.SinkPort != ServerSinkPort)
            return;

        if (!HasComp<QuantumServerComponent>(args.Source))
            return;

        ent.Comp.LinkedServer = args.Source;
    }

    private void OnPortDisconnected(Entity<NetpodComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ServerSinkPort || ent.Comp.LinkedServer != args.RemovedPortUid)
            return;

        ent.Comp.LinkedServer = null;
    }

    private void RefreshLinkedServer(Entity<NetpodComponent> ent)
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent.Owner, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!HasComp<QuantumServerComponent>(source))
                continue;

            ent.Comp.LinkedServer = source;
            return;
        }

        ent.Comp.LinkedServer = null;
    }

    private void AutoConnectDelay(EntityUid podUid, EntityUid user, TimeSpan delay)
    {
        Timer.Spawn(delay, () =>
        {
            if (!TryComp<NetpodComponent>(podUid, out var pod) || pod.Occupant != user)
                return;

            if (TryComp<ApcPowerReceiverComponent>(podUid, out var power) && !power.Powered)
            {
                _popup.PopupEntity(Loc.GetString("bitrunning-netpod-no-power"), podUid, user);
                return;
            }

            TryAutoConnect((podUid, pod), user);
        });
    }

    private void TryAutoConnect(Entity<NetpodComponent> ent, EntityUid user)
    {
        var serverUid = ResolveServer(ent);
        if (serverUid == null)
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-no-server"), ent, user);
            return;
        }

        if (_server.TryConnectRunner(serverUid.Value, ent.Owner, user))
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-connected"), ent, user);
            return;
        }

        _popup.PopupEntity(Loc.GetString("bitrunning-netpod-connect-failed"), ent, user);
    }

    public void UpdateVisuals(Entity<NetpodComponent> ent)
    {
        var state = ent.Comp.Avatar != null
            ? ent.Comp.Occupant != null
                ? NetpodVisualState.Active
                : NetpodVisualState.OpenActive
            : ent.Comp.Occupant != null
                ? NetpodVisualState.Closed
                : NetpodVisualState.Open;

        SetVisualState(ent, state);
    }

    private void SetVisualState(Entity<NetpodComponent> ent, NetpodVisualState state)
    {
        _appearance.SetData(ent, NetpodVisuals.State, state);

        if (!TryComp<PhysicsComponent>(ent, out var physics))
            return;

        var canCollide = state is NetpodVisualState.Closed or NetpodVisualState.Active;
        _physics.SetCanCollide(ent, canCollide, body: physics);
    }

    private string GetLoadoutDisplayName(ProtoId<StartingGearPrototype> loadoutId)
    {
        _startingGearToJobName ??= BuildStartingGearLookup();
        if (_startingGearToJobName.TryGetValue(loadoutId, out var name))
            return name;

        var fallbackKey = $"loadout-{loadoutId.ToString().ToLowerInvariant()}";
        return _loc.TryGetString(fallbackKey, out var localizedLoadoutName)
            ? localizedLoadoutName
            : loadoutId.ToString();
    }

    private Dictionary<ProtoId<StartingGearPrototype>, string> BuildStartingGearLookup()
    {
        var lookup = new Dictionary<ProtoId<StartingGearPrototype>, string>();
        foreach (var job in _prototype.EnumeratePrototypes<JobPrototype>())
        {
            if (job.StartingGear == null || lookup.ContainsKey(job.StartingGear.Value))
                continue;

            lookup[job.StartingGear.Value] = _loc.TryGetString(job.Name, out var localizedJobName)
                ? localizedJobName
                : job.Name;
        }

        return lookup;
    }
}
