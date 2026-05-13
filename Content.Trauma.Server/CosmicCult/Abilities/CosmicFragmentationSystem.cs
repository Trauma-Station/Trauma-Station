// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Popups;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Trauma.Common.Silicon;
using Content.Trauma.Shared.CosmicCult;
using Content.Trauma.Shared.CosmicCult.Components;
using Content.Trauma.Shared.Silicons;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.CosmicCult.Abilities;

public sealed partial class CosmicFragmentationSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private CosmicCultSystem _cult = default!;
    [Dependency] private CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ActionsSystem _actions = default!;

    private ProtoId<RadioChannelPrototype> _cultRadio = "CosmicRadio";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawUpdaterComponent, AILawUpdatedEvent>(OnLawInserted);

        SubscribeLocalEvent<BorgChassisComponent, MalignFragmentationEvent>(OnFragmentBorg);
        SubscribeLocalEvent<SiliconLawUpdaterComponent, MalignFragmentationEvent>(OnFragmentAi);

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicFragmentation>(OnCosmicFragmentation);
    }

    private void OnCosmicFragmentation(Entity<CosmicCultComponent> ent, ref EventCosmicFragmentation args)
    {
        if (args.Handled || HasComp<ActiveNPCComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-generic-fail"), ent, ent);
            return;
        }

        var evt = new MalignFragmentationEvent(ent, args.Target);
        RaiseLocalEvent(args.Target, ref evt);
        if (evt.Canceled) return;

        args.Handled = true;
        _cult.MalignEcho(ent);
        ent.Comp.CosmicFragmentationActionEntity = null;
        _actions.RemoveAction(ent.Owner, args.Action.Owner);
    }

    private void OnFragmentBorg(Entity<BorgChassisComponent> ent, ref MalignFragmentationEvent args)
    {
        if (_cultRule.AssociatedGamerule(args.User) is { } cult && Exists(cult.Comp.ActiveChantry))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-chantry-active"), args.User, args.User);
            args.Canceled = true;
            return;
        }

        var chantry = Spawn("CosmicBorgChantry", Transform(ent).Coordinates);
        EnsureComp<CosmicChantryComponent>(chantry, out var chantryComponent);
        chantryComponent.Container = _container.EnsureContainer<ContainerSlot>(chantry, chantryComponent.ContainerId);
        _container.Insert(ent.Owner, chantryComponent.Container);
        _cultRule.TransferCultAssociation(args.User, chantry);
        if (chantryComponent.Victim is not { } victim) return;
        var mins = chantryComponent.EventTime.Minutes;
        var secs = chantryComponent.EventTime.Seconds;
        _antag.SendBriefing(victim, Loc.GetString("cosmiccult-silicon-chantry-briefing", ("minutesandseconds", $"{mins} minutes and {secs} seconds")), Color.FromHex("#4cabb3"), null);
    }

    private void OnFragmentAi(Entity<SiliconLawUpdaterComponent> ent, ref MalignFragmentationEvent args)
    {
        var lawboard = Spawn("CosmicCultLawBoard", Transform(args.Target).Coordinates);
        _container.TryGetContainer(args.Target, "circuit_holder", out var container);
        if (container == null)
            return;
        _container.EmptyContainer(container, true);
        _container.Insert(lawboard, container, Transform(args.Target), true);
    }

    private void OnLawInserted(Entity<SiliconLawUpdaterComponent> ent, ref AILawUpdatedEvent args)
    {
        if (!TryComp<IntrinsicRadioTransmitterComponent>(ent, out var radio) || !TryComp<ActiveRadioComponent>(ent, out var transmitter))
            return;
        if (ent.Comp.LastLawset.Id == "CosmicCultLaws")
        {
            radio.Channels.Add(_cultRadio);
            transmitter.Channels.Add(_cultRadio);
            _antag.SendBriefing(ent, Loc.GetString("cosmiccult-silicon-subverted-briefing"), Color.FromHex("#4cabb3"), null);
        }
        else
        {
            radio.Channels.Remove(_cultRadio);
            transmitter.Channels.Remove(_cultRadio);
        }
    }
}

[ByRefEvent]
public record struct MalignFragmentationEvent(Entity<CosmicCultComponent> User, EntityUid Target, bool Canceled = false);
