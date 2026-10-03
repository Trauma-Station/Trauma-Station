
// <Trauma>
using Content.Trauma.Common.Cargo;
// </Trauma>

using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Content.Server.Cargo.Components;
using Content.Server.NameIdentifier;

using Content.Shared.Access.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.NameIdentifier;
using Content.Shared.Paper;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;

using JetBrains.Annotations;

using Robust.Server.Containers;

using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private NameIdentifierSystem _nameIdentifier = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSys = default!;

    [Dependency] private EntityQuery<StackComponent> _stackQuery = default!;
    [Dependency] private EntityQuery<ContainerManagerComponent> _containerManagerQuery = default!;
    [Dependency] private EntityQuery<CargoBountyLabelComponent> _cargoBountyLabelQuery = default!;

    private static readonly ProtoId<NameIdentifierGroupPrototype> BountyNameIdentifierGroup = "Bounty";

    /*
     * Faction bounty prototypes.
     *
     * Они являются постоянными контрактами.
     * Если игрок нажимает Skip, сам контракт не удаляется.
     * Благодаря этому он автоматически остаётся в списке
     * и после обновления снова находится наверху.
     */
    private static readonly string[] FactionBountyIds =
    {
        "FactionWeaponPistolFlintlock",
        "FactionWeaponShotgunImprovised",
    };

    private void InitializeBounty()
    {
        Logger.InfoS(
            "CargoReputation",
            "Cargo bounty / faction reputation system initialized.");

        SubscribeLocalEvent<CargoBountyConsoleComponent, BoundUIOpenedEvent>(
            OnBountyConsoleOpened);

        SubscribeLocalEvent<CargoBountyConsoleComponent, ExaminedEvent>(
            OnBountyConsoleExamined);

        SubscribeLocalEvent<CargoBountyConsoleComponent, BountyPrintLabelMessage>(
            OnPrintLabelMessage);

        SubscribeLocalEvent<CargoBountyConsoleComponent, BountySkipMessage>(
            OnSkipBountyMessage);

        SubscribeLocalEvent<CargoBountyLabelComponent, PriceCalculationEvent>(
            OnGetBountyPrice);

        SubscribeLocalEvent<EntitySoldEvent>(OnSold);

        SubscribeLocalEvent<StationCargoBountyDatabaseComponent, MapInitEvent>(
            OnMapInit);
    }

    // ============================================================
    // UI
    // ============================================================

    private void OnBountyConsoleOpened(
        EntityUid uid,
        CargoBountyConsoleComponent component,
        BoundUIOpenedEvent args)
    {
        UpdateBountyConsole(uid);
    }

    private void UpdateBountyConsole(EntityUid uid)
    {
        if (_station.GetOwningStation(uid) is not { } station)
        {
            Logger.WarningS(
                "CargoReputation",
                $"Unable to find owning station for bounty console {uid}.");

            return;
        }

        if (!TryComp<StationCargoBountyDatabaseComponent>(
                station,
                out var database))
        {
            Logger.WarningS(
                "CargoReputation",
                $"Station {station} has no bounty database.");

            return;
        }

        EnsureFactionBounties(station, database);

        var bounties = GetSortedBounties(database);

        var reputation = EnsureComp<CargoReputationComponent>(station);
        var militaryReputation = reputation.MilitaryReputation;
        var medicalReputation = reputation.MedicalReputation;
        var serviceReputation = reputation.ServiceReputation;

        var untilNextSkip =
            database.NextSkipTime - Timing.CurTime;

        _uiSystem.SetUiState(
            uid,
            CargoConsoleUiKey.Bounty,
            new CargoBountyConsoleState(
                bounties,
                database.History,
                untilNextSkip,
                militaryReputation,
                medicalReputation,
                serviceReputation));
    }

    private void UpdateBountyConsolesForStation(EntityUid station)
    {
        var query =
            EntityQueryEnumerator<
                CargoBountyConsoleComponent,
                UserInterfaceComponent>();

        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;

            if (!TryComp<StationCargoBountyDatabaseComponent>(
                    station,
                    out var database))
            {
                continue;
            }

            EnsureFactionBounties(station, database);

            var bounties = GetSortedBounties(database);

            var reputation = EnsureComp<CargoReputationComponent>(station);
            var militaryReputation = reputation.MilitaryReputation;
            var medicalReputation = reputation.MedicalReputation;
            var serviceReputation = reputation.ServiceReputation;

            var untilNextSkip =
                database.NextSkipTime - Timing.CurTime;

            _uiSystem.SetUiState(
                (uid, ui),
                CargoConsoleUiKey.Bounty,
                new CargoBountyConsoleState(
                    bounties,
                    database.History,
                    untilNextSkip,
                    militaryReputation,
                    medicalReputation,
                    serviceReputation));
        }
    }

    private void OnBountyConsoleExamined(
        EntityUid uid,
        CargoBountyConsoleComponent component,
        ExaminedEvent args)
    {
        if (_station.GetOwningStation(uid) is not { } station)
            return;

        var reputation = EnsureComp<CargoReputationComponent>(station);

        args.PushMarkup(
            Loc.GetString(
                "cargo-bounty-console-reputation",
                ("militaryXp", reputation.MilitaryReputation),
                ("medicalXp", reputation.MedicalReputation),
                ("serviceXp", reputation.ServiceReputation)));
    }

    /*
     * Faction bounty всегда идут первыми.
     *
     * Важно: проверяется prototype ID bounty.Bounty,
     * а не bounty.Id.
     */
    private List<CargoBountyData> GetSortedBounties(
        StationCargoBountyDatabaseComponent database)
    {
        var faction = new List<CargoBountyData>();
        var regular = new List<CargoBountyData>();

        foreach (var bounty in database.Bounties)
        {
            if (IsFactionBounty(bounty))
                faction.Add(bounty);
            else
                regular.Add(bounty);
        }

        faction.AddRange(regular);

        return faction;
    }

    private bool IsFactionBounty(CargoBountyData bounty)
    {
        var bountyId = bounty.Bounty.ToString();

        return FactionBountyIds.Contains(
            bountyId,
            StringComparer.OrdinalIgnoreCase);
    }

    // ============================================================
    // Faction bounty management
    // ============================================================

    private void EnsureFactionBounties(
        EntityUid station,
        StationCargoBountyDatabaseComponent database)
    {
        foreach (var bountyId in FactionBountyIds)
        {
            if (database.Bounties.Any(
                    b => b.Bounty == bountyId))
            {
                continue;
            }

            if (!ProtoMan.TryIndex<CargoBountyPrototype>(
                    bountyId,
                    out var prototype))
            {
                Logger.WarningS(
                    "CargoReputation",
                    $"Faction bounty prototype '{bountyId}' was not found.");

                continue;
            }

            /*
             * ВАЖНО:
             *
             * TryAddBounty() vanilla проверяет MaxBounties.
             * Поэтому faction bounty не должны занимать обычные
             * bounty slots.
             *
             * Если MaxBounties уже заполнен, временно добавляем
             * faction bounty напрямую через отдельный метод.
             */
            AddFactionBounty(
                station,
                database,
                prototype);
        }
    }

    private bool AddFactionBounty(
        EntityUid station,
        StationCargoBountyDatabaseComponent database,
        CargoBountyPrototype prototype)
    {
        if (database.Bounties.Any(
                b => b.Bounty == prototype.ID))
        {
            return true;
        }

        _nameIdentifier.GenerateUniqueName(
            station,
            BountyNameIdentifierGroup,
            out var randomVal);

        /*
         * Используем тот же ModifyBountyRewardEvent,
         * что и vanilla cargo system.
         */
        var ev = new ModifyBountyRewardEvent(prototype.Reward);
        RaiseLocalEvent(station, ref ev);

        var bounty = new CargoBountyData(
            prototype,
            randomVal,
            ev.Reward);

        if (database.Bounties.Any(
                b => b.Id == bounty.Id))
        {
            Logger.WarningS(
                "CargoReputation",
                $"Unable to add faction bounty {prototype.ID}: duplicate ID {bounty.Id}.");

            return false;
        }

        database.Bounties.Add(bounty);
        database.TotalBounties++;

        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"Added faction bounty \"{prototype.ID}\" " +
            $"(id:{bounty.Id}) to station {ToPrettyString(station)}");

        return true;
    }

    // ============================================================
    // Printing
    // ============================================================

    private void OnPrintLabelMessage(
        EntityUid uid,
        CargoBountyConsoleComponent component,
        BountyPrintLabelMessage args)
    {
        if (Timing.CurTime < component.NextPrintTime)
            return;

        if (_station.GetOwningStation(uid) is not { } station)
            return;

        if (!TryGetBountyFromId(
                station,
                args.BountyId,
                out var bounty))
        {
            return;
        }

        var label = Spawn(
            component.BountyLabelId,
            Transform(uid).Coordinates);

        component.NextPrintTime =
            Timing.CurTime + component.PrintDelay;

        SetupBountyLabel(
            label,
            station,
            bounty.Value);

        _audio.PlayPvs(
            component.PrintSound,
            uid);
    }

    // ============================================================
    // Skip
    // ============================================================

    private void OnSkipBountyMessage(
        EntityUid uid,
        CargoBountyConsoleComponent component,
        BountySkipMessage args)
    {
        if (_station.GetOwningStation(uid) is not { } station ||
            !TryComp<StationCargoBountyDatabaseComponent>(
                station,
                out var database))
        {
            return;
        }

        if (Timing.CurTime < database.NextSkipTime)
            return;

        if (!TryGetBountyFromId(
                station,
                args.BountyId,
                out var bounty))
        {
            return;
        }

        if (args.Actor is not { Valid: true } mob)
            return;

        if (TryComp<AccessReaderComponent>(
                uid,
                out var accessReaderComponent) &&
            !_accessReaderSystem.IsAllowed(
                mob,
                uid,
                accessReaderComponent))
        {
            if (Timing.CurTime >= component.NextDenySoundTime)
            {
                component.NextDenySoundTime =
                    Timing.CurTime + component.DenySoundDelay;

                _audio.PlayPvs(
                    component.DenySound,
                    uid);
            }

            return;
        }

        /*
         * Faction bounty НЕ удаляем.
         *
         * Поэтому после Skip он остаётся в database.Bounties.
         * Следующее обновление UI снова сортирует его наверх.
         */
        if (!IsFactionBounty(bounty.Value))
        {
            if (!TryRemoveBounty(
                    station,
                    bounty.Value,
                    true,
                    args.Actor))
            {
                return;
            }

            FillBountyDatabase(
                station,
                database,
                updateUi: false);
        }
        else
        {
            /*
             * Записываем skip в историю, но сам контракт
             * оставляем активным.
             */
            string? actorName = null;

            if (args.Actor is { Valid: true } actor)
            {
                actorName =
                    _identity.GetIdentityShortInfo(
                        actor,
                        station);
            }

            database.History.Add(
                new CargoBountyHistoryData(
                    bounty.Value,
                    CargoBountyHistoryData.BountyResult.Skipped,
                    Timing.CurTime,
                    actorName));
        }

        database.NextSkipTime =
            Timing.CurTime + database.SkipDelay;

        UpdateBountyConsolesForStation(station);

        _audio.PlayPvs(
            component.SkipSound,
            uid);
    }

    // ============================================================
    // Labels
    // ============================================================

    public void SetupBountyLabel(
        EntityUid uid,
        EntityUid stationId,
        CargoBountyData bounty,
        PaperComponent? paper = null,
        CargoBountyLabelComponent? label = null)
    {
        if (!Resolve(
                uid,
                ref paper,
                ref label) ||
            !ProtoMan.Resolve<CargoBountyPrototype>(
                bounty.Bounty,
                out var prototype))
        {
            return;
        }

        label.Id = bounty.Id;
        label.AssociatedStationId = stationId;

        var msg = new FormattedMessage();

        msg.AddMarkupOrThrow(
            Loc.GetString(
                "bounty-manifest-header",
                ("id", bounty.Id)));

        msg.PushNewline();

        msg.AddMarkupOrThrow(
            Loc.GetString(
                "bounty-manifest-list-start"));

        msg.PushNewline();

        foreach (var entry in prototype.Entries)
        {
            msg.AddMarkupOrThrow(
                $"- {Loc.GetString(
                    "bounty-console-manifest-entry",
                    ("amount", entry.Amount),
                    ("item", Loc.GetString(entry.Name)))}");

            msg.PushNewline();
        }

        // <Trauma>
        var ev = new ModifyBountyRewardEvent(
            prototype.Reward);

        RaiseLocalEvent(
            stationId,
            ref ev);

        msg.AddMarkupOrThrow(
            Loc.GetString(
                "bounty-console-manifest-reward",
                ("reward", ev.Reward)));
        // </Trauma>

        _paperSystem.SetContent(
            (uid, paper),
            msg.ToMarkup());
    }

    // ============================================================
    // Bounty price
    // ============================================================

    private void OnGetBountyPrice(
        EntityUid uid,
        CargoBountyLabelComponent component,
        ref PriceCalculationEvent args)
    {
        if (args.Handled || component.Calculating)
            return;

        if (!_container.TryGetContainingContainer(
                (uid, null, null),
                out var container) ||
            container.ID != LabelSystem.ContainerName)
        {
            return;
        }

        if (component.AssociatedStationId is not { } station ||
            !TryComp<StationCargoBountyDatabaseComponent>(
                station,
                out var database))
        {
            return;
        }

        if (database.CheckedBounties.Contains(component.Id))
            return;

        if (!TryGetBountyFromId(
                station,
                component.Id,
                out var bounty,
                database))
        {
            return;
        }

        if (!ProtoMan.Resolve(
                bounty.Value.Bounty,
                out var bountyPrototype) ||
            !IsBountyComplete(
                container.Owner,
                bountyPrototype))
        {
            return;
        }

        database.CheckedBounties.Add(component.Id);

        args.Handled = true;

        component.Calculating = true;

        // <Trauma>
        var ev = new ModifyBountyRewardEvent(
            bountyPrototype.Reward);

        RaiseLocalEvent(
            station,
            ref ev);

        args.Price =
            ev.Reward -
            _pricing.GetPrice(container.Owner);
        // </Trauma>

        component.Calculating = false;
    }

    // ============================================================
    // Completion / reputation
    // ============================================================

    private void OnSold(ref EntitySoldEvent args)
    {
        foreach (var sold in args.Sold)
        {
            if (!TryGetBountyLabel(
                    sold,
                    out _,
                    out var label))
            {
                continue;
            }

            if (label.AssociatedStationId is not { } station ||
                !TryGetBountyFromId(
                    station,
                    label.Id,
                    out var bounty))
            {
                continue;
            }

            if (!IsBountyComplete(
                    sold,
                    bounty.Value))
            {
                continue;
            }

            /*
             * Получаем именно prototype заказа.
             *
             * bounty.Id = уникальный ID экземпляра.
             * bounty.Bounty = ID CargoBountyPrototype.
             */
            if (!ProtoMan.Resolve(
                    bounty.Value.Bounty,
                    out var bountyPrototype))
            {
                Logger.WarningS(
                    "CargoReputation",
                    $"Unable to resolve bounty prototype {bounty.Value.Bounty}.");

                continue;
            }

            /*
             * Репутация начисляется ТОЛЬКО для наших faction
             * контрактов.
             *
             * Обычный vanilla bounty ничего не даёт.
             */
            ProcessReputationForBounty(
                station,
                bountyPrototype);

            TryRemoveBounty(
                station,
                bounty.Value,
                false);

            FillBountyDatabase(
                station,
                outOfBandComponent: null);

            EnsureFactionBounties(
                station,
                GetStationBountyDatabase(station)!);

            UpdateBountyConsolesForStation(
                station);

            _adminLogger.Add(
                LogType.Action,
                LogImpact.Low,
                $"Bounty \"{bounty.Value.Bounty}\" " +
                $"(id:{bounty.Value.Id}) was fulfilled");
        }
    }

    private void ProcessReputationForBounty(
        EntityUid station,
        CargoBountyPrototype bounty)
    {
        var reputation = EnsureComp<CargoReputationComponent>(station);

        var type = GetReputationType(
            bounty.ID);

        switch (type)
        {
            case CargoReputationType.Military:
                reputation.MilitaryReputation += 5f;

                Logger.InfoS(
                    "CargoReputation",
                    $"Military reputation +5. " +
                    $"New value: {reputation.MilitaryReputation}");

                break;

            case CargoReputationType.Medical:
                reputation.MedicalReputation += 5f;

                Logger.InfoS(
                    "CargoReputation",
                    $"Medical reputation +5. " +
                    $"New value: {reputation.MedicalReputation}");

                break;

            case CargoReputationType.Service:
                reputation.ServiceReputation += 5f;

                Logger.InfoS(
                    "CargoReputation",
                    $"Service reputation +5. " +
                    $"New value: {reputation.ServiceReputation}");

                break;

            case CargoReputationType.None:
            default:
                /*
                 * Не делаем fallback в Service!
                 *
                 * Это принципиально:
                 * обычный bounty не должен давать репутацию.
                 */
                return;
        }

        Dirty(
            station,
            reputation);
    }

    private CargoReputationType GetReputationType(
        ProtoId<CargoBountyPrototype> bountyId)
    {
        return bountyId.ToString() switch
        {
            "FactionWeaponPistolFlintlock" =>
                CargoReputationType.Military,

            "FactionWeaponShotgunImprovised" =>
                CargoReputationType.Military,

            // ====================================================
            // Добавляй сюда медицинские bounty:
            //
            // "FactionMedicalMedkit" => CargoReputationType.Medical,
            //
            // ====================================================

            // ====================================================
            // И сервисные:
            //
            // "FactionServiceDonuts" => CargoReputationType.Service,
            //
            // ====================================================

            _ => CargoReputationType.None
        };
    }

    private enum CargoReputationType
    {
        None,
        Military,
        Medical,
        Service
    }

    // ============================================================
    // Labels
    // ============================================================

    private bool TryGetBountyLabel(
        EntityUid uid,
        [NotNullWhen(true)] out EntityUid? labelEnt,
        [NotNullWhen(true)] out CargoBountyLabelComponent? labelComp)
    {
        labelEnt = null;
        labelComp = null;

        if (!_containerManagerQuery.TryGetComponent(
                uid,
                out var containerMan))
        {
            return false;
        }

        if (!_container.TryGetContainer(
                uid,
                LabelSystem.ContainerName,
                out var container,
                containerMan))
        {
            return false;
        }

        if (container.ContainedEntities.FirstOrNull()
                is not { } label ||
            !_cargoBountyLabelQuery.TryGetComponent(
                label,
                out var component))
        {
            return false;
        }

        labelEnt = label;
        labelComp = component;

        return true;
    }

    // ============================================================
    // Map initialization
    // ============================================================

    private void OnMapInit(
        EntityUid uid,
        StationCargoBountyDatabaseComponent component,
        MapInitEvent args)
    {
        /*
         * Сначала создаём vanilla bounty.
         */
        FillBountyDatabase(
            uid,
            component,
            updateUi: false);

        /*
         * Затем гарантируем faction bounty.
         */
        EnsureFactionBounties(
            uid,
            component);

        UpdateBountyConsolesForStation(
            uid);

        Logger.InfoS(
            "CargoReputation",
            "[MAP INIT] Faction bounty contracts initialized.");
    }

    // ============================================================
    // Fill database
    // ============================================================

    public void FillBountyDatabase(
        EntityUid uid,
        StationCargoBountyDatabaseComponent? component = null,
        bool updateUi = true,
        StationCargoBountyDatabaseComponent? outOfBandComponent = null)
    {
        if (component == null)
        {
            if (!TryComp<
                    StationCargoBountyDatabaseComponent>(
                    uid,
                    out var resolved))
            {
                return;
            }

            component = resolved;
        }

        while (component.Bounties.Count(
                   b => !IsFactionBounty(b))
               < component.MaxBounties)
        {
            if (!TryAddBounty(
                    uid,
                    component))
            {
                break;
            }
        }

        EnsureFactionBounties(
            uid,
            component);

        if (updateUi)
            UpdateBountyConsolesForStation(uid);
    }

    public void RerollBountyDatabase(
        Entity<StationCargoBountyDatabaseComponent?> entity)
    {
        if (!Resolve(
                entity,
                ref entity.Comp))
        {
            return;
        }

        /*
         * Полностью очищаем обычные bounty.
         *
         * Faction bounty тоже могут быть удалены vanilla
         * reroll-логикой, поэтому после очистки гарантируем
         * их наличие снова.
         */
        entity.Comp.Bounties.Clear();

        entity.Comp.CheckedBounties.Clear();

        FillBountyDatabase(
            entity,
            entity.Comp,
            updateUi: true);
    }

    // ============================================================
    // Bounty completion
    // ============================================================

    public bool IsBountyComplete(
        EntityUid container,
        out HashSet<EntityUid> bountyEntities)
    {
        if (!TryGetBountyLabel(
                container,
                out _,
                out var component))
        {
            bountyEntities = new();
            return false;
        }

        var station = component.AssociatedStationId;

        if (station == null)
        {
            bountyEntities = new();
            return false;
        }

        if (!TryGetBountyFromId(
                station.Value,
                component.Id,
                out var bounty))
        {
            bountyEntities = new();
            return false;
        }

        return IsBountyComplete(
            container,
            bounty.Value,
            out bountyEntities);
    }

    public bool IsBountyComplete(
        EntityUid container,
        CargoBountyData data)
    {
        return IsBountyComplete(
            container,
            data,
            out _);
    }

    public bool IsBountyComplete(
        EntityUid container,
        CargoBountyData data,
        out HashSet<EntityUid> bountyEntities)
    {
        if (!ProtoMan.Resolve(
                data.Bounty,
                out var proto))
        {
            bountyEntities = new();
            return false;
        }

        return IsBountyComplete(
            container,
            proto.Entries,
            out bountyEntities);
    }

    public bool IsBountyComplete(
        EntityUid container,
        string id)
    {
        if (!ProtoMan.TryIndex<
                CargoBountyPrototype>(
                id,
                out var proto))
        {
            return false;
        }

        return IsBountyComplete(
            container,
            proto.Entries);
    }

    public bool IsBountyComplete(
        EntityUid container,
        ProtoId<CargoBountyPrototype> prototypeId)
    {
        var prototype =
            ProtoMan.Index(prototypeId);

        return IsBountyComplete(
            container,
            prototype.Entries);
    }

    public bool IsBountyComplete(
        EntityUid container,
        CargoBountyPrototype prototype)
    {
        return IsBountyComplete(
            container,
            prototype.Entries);
    }

    public bool IsBountyComplete(
        EntityUid container,
        IEnumerable<CargoBountyItemEntry> entries)
    {
        return IsBountyComplete(
            container,
            entries,
            out _);
    }

    public bool IsBountyComplete(
        EntityUid container,
        IEnumerable<CargoBountyItemEntry> entries,
        out HashSet<EntityUid> bountyEntities)
    {
        return IsBountyComplete(
            GetBountyEntities(container),
            entries,
            out bountyEntities);
    }

    public bool IsValidBountyEntry(
        EntityUid entity,
        CargoBountyItemEntry entry)
    {
        if (!_whitelistSys.IsValid(
                entry.Whitelist,
                entity))
        {
            return false;
        }

        if (entry.Blacklist != null &&
            _whitelistSys.IsValid(
                entry.Blacklist,
                entity))
        {
            return false;
        }

        return true;
    }

    public bool IsBountyComplete(
        HashSet<EntityUid> entities,
        IEnumerable<CargoBountyItemEntry> entries,
        out HashSet<EntityUid> bountyEntities)
    {
        bountyEntities = new();

        foreach (var entry in entries)
        {
            var count = 0;

            var temp = new HashSet<EntityUid>();

            foreach (var entity in entities)
            {
                if (!IsValidBountyEntry(
                        entity,
                        entry))
                {
                    continue;
                }

                count +=
                    _stackQuery.CompOrNull(entity)?.Count
                    ?? 1;

                temp.Add(entity);

                if (count >= entry.Amount)
                    break;
            }

            if (count < entry.Amount)
                return false;

            foreach (var ent in temp)
            {
                entities.Remove(ent);
                bountyEntities.Add(ent);
            }
        }

        return true;
    }

    private HashSet<EntityUid> GetBountyEntities(
        EntityUid uid)
    {
        var entities = new HashSet<EntityUid>
        {
            uid
        };

        if (!TryComp<
                ContainerManagerComponent>(
                uid,
                out var containers))
        {
            return entities;
        }

        foreach (var container in containers.Containers.Values)
        {
            foreach (var ent in container.ContainedEntities)
            {
                if (_cargoBountyLabelQuery.HasComponent(ent))
                    continue;

                var children =
                    GetBountyEntities(ent);

                foreach (var child in children)
                {
                    entities.Add(child);
                }
            }
        }

        return entities;
    }

    // ============================================================
    // Add bounty
    // ============================================================

    [PublicAPI]
    public bool TryAddBounty(
        EntityUid uid,
        StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(
                uid,
                ref component))
        {
            return false;
        }

        var allBounties =
            ProtoMan
                .EnumeratePrototypes<CargoBountyPrototype>()
                .Where(p => p.Group == component.Group)
                /*
                 * Faction bounty не должны попадать
                 * в случайный vanilla pool.
                 */
                .Where(p => !IsFactionBountyPrototype(p.ID))
                .ToList();

        var filteredBounties =
            new List<CargoBountyPrototype>();

        foreach (var proto in allBounties)
        {
            if (component.Bounties.Any(
                    b => b.Bounty == proto.ID))
            {
                continue;
            }

            filteredBounties.Add(proto);
        }

        var pool =
            filteredBounties.Count == 0
                ? allBounties
                : filteredBounties;

        if (pool.Count == 0)
            return false;

        var bounty =
            _random.Pick(pool);

        return TryAddBounty(
            uid,
            bounty,
            component);
    }

    private bool IsFactionBountyPrototype(
        ProtoId<CargoBountyPrototype> id)
    {
        return FactionBountyIds.Contains(
            id.ToString(),
            StringComparer.OrdinalIgnoreCase);
    }

    [PublicAPI]
    public bool TryAddBounty(
        EntityUid uid,
        string bountyId,
        StationCargoBountyDatabaseComponent? component = null)
    {
        if (!ProtoMan.TryIndex<
                CargoBountyPrototype>(
                bountyId,
                out var bounty))
        {
            return false;
        }

        return TryAddBounty(
            uid,
            bounty,
            component);
    }

    public bool TryAddBounty(
        EntityUid uid,
        CargoBountyPrototype bounty,
        StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(
                uid,
                ref component))
        {
            return false;
        }

        /*
         * Не даём vanilla методу добавлять faction bounty.
         */
        if (IsFactionBountyPrototype(bounty.ID))
        {
            return AddFactionBounty(
                uid,
                component,
                bounty);
        }

        /*
         * MaxBounties относится только к обычным bounty.
         */
        var regularCount =
            component.Bounties.Count(
                b => !IsFactionBounty(b));

        if (regularCount >= component.MaxBounties)
            return false;

        _nameIdentifier.GenerateUniqueName(
            uid,
            BountyNameIdentifierGroup,
            out var randomVal);

        // <Trauma>
        var ev = new ModifyBountyRewardEvent(
            bounty.Reward);

        RaiseLocalEvent(
            uid,
            ref ev);

        var newBounty =
            new CargoBountyData(
                bounty,
                randomVal,
                ev.Reward);
        // </Trauma>

        if (component.Bounties.Any(
                b => b.Id == newBounty.Id))
        {
            Log.Warning(
                $"Failed to add bounty {newBounty.Id} " +
                "because another one with the same ID already existed!");

            return false;
        }

        component.Bounties.Add(
            newBounty);

        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"Added bounty \"{bounty.ID}\" " +
            $"(id:{component.TotalBounties}) " +
            $"to station {ToPrettyString(uid)}");

        component.TotalBounties++;

        return true;
    }

    // ============================================================
    // Remove
    // ============================================================

    [PublicAPI]
    public bool TryRemoveBounty(
        Entity<StationCargoBountyDatabaseComponent?> ent,
        string dataId,
        bool skipped,
        EntityUid? actor = null)
    {
        if (!TryGetBountyFromId(
                ent.Owner,
                dataId,
                out var data,
                ent.Comp))
        {
            return false;
        }

        return TryRemoveBounty(
            ent,
            data.Value,
            skipped,
            actor);
    }

    public bool TryRemoveBounty(
        Entity<StationCargoBountyDatabaseComponent?> ent,
        CargoBountyData data,
        bool skipped,
        EntityUid? actor = null)
    {
        if (!Resolve(
                ent,
                ref ent.Comp))
        {
            return false;
        }

        for (var i = 0;
             i < ent.Comp.Bounties.Count;
             i++)
        {
            if (ent.Comp.Bounties[i].Id != data.Id)
                continue;

            string? actorName = null;

            if (actor != null)
            {
                actorName =
                    _identity.GetIdentityShortInfo(
                        actor.Value,
                        ent.Owner);
            }

            ent.Comp.History.Add(
                new CargoBountyHistoryData(
                    data,
                    skipped
                        ? CargoBountyHistoryData.BountyResult.Skipped
                        : CargoBountyHistoryData.BountyResult.Completed,
                    Timing.CurTime,
                    actorName));

            ent.Comp.Bounties.RemoveAt(i);

            return true;
        }

        return false;
    }

    // ============================================================
    // Find bounty
    // ============================================================

    public bool TryGetBountyFromId(
        EntityUid uid,
        string id,
        [NotNullWhen(true)] out CargoBountyData? bounty,
        StationCargoBountyDatabaseComponent? component = null)
    {
        bounty = null;

        if (!Resolve(
                uid,
                ref component))
        {
            return false;
        }

        foreach (var bountyData in component.Bounties)
        {
            if (bountyData.Id != id)
                continue;

            bounty = bountyData;
            break;
        }

        return bounty != null;
    }

    // ============================================================
    // Helpers
    // ============================================================

    private StationCargoBountyDatabaseComponent?
        GetStationBountyDatabase(EntityUid station)
    {
        return TryComp<
            StationCargoBountyDatabaseComponent>(
            station,
            out var database)
            ? database
            : null;
    }

    /*
     * Совместимый wrapper, если в других partial-файлах
     * вызывается старый UpdateBountyConsoles().
     */
    public void UpdateBountyConsoles()
    {
        var query =
            EntityQueryEnumerator<
                CargoBountyConsoleComponent,
                UserInterfaceComponent>();

        while (query.MoveNext(
                   out var uid,
                   out _,
                   out var ui))
        {
            if (_station.GetOwningStation(uid)
                is not { } station)
            {
                continue;
            }

            if (!TryComp<
                    StationCargoBountyDatabaseComponent>(
                    station,
                    out var database))
            {
                continue;
            }

            EnsureFactionBounties(
                station,
                database);

            var bounties =
                GetSortedBounties(database);

            var reputation = EnsureComp<CargoReputationComponent>(station);
            var military = reputation.MilitaryReputation;
            var medical = reputation.MedicalReputation;
            var service = reputation.ServiceReputation;

            var untilNextSkip =
                database.NextSkipTime -
                Timing.CurTime;

            _uiSystem.SetUiState(
                (uid, ui),
                CargoConsoleUiKey.Bounty,
                new CargoBountyConsoleState(
                    bounties,
                    database.History,
                    untilNextSkip,
                    military,
                    medical,
                    service));
        }
    }

    private void UpdateBounty()
    {
        var query =
            EntityQueryEnumerator<
                StationCargoBountyDatabaseComponent>();

        while (query.MoveNext(
                   out var bountyDatabase))
        {
            bountyDatabase.CheckedBounties.Clear();
        }
    }
}