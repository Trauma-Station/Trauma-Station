// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Containers;

/// <summary>
/// Moved out of Content.Server and predicted, its 2026.
/// </summary>
public sealed class ThrowInsertContainerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrowInsertContainerComponent, ThrowHitByEvent>(OnThrowCollide);
    }

    private void OnThrowCollide(Entity<ThrowInsertContainerComponent> ent, ref ThrowHitByEvent args)
    {
        var container = _container.GetContainer(ent, ent.Comp.ContainerId);
        if (!_container.CanInsert(args.Thrown, container))
            return;

        var beforeThrowArgs = new BeforeThrowInsertEvent(args.Thrown);
        RaiseLocalEvent(ent, ref beforeThrowArgs);

        if (beforeThrowArgs.Cancelled)
            return;

        var ev = new ModifyThrowInsertChanceEvent(ent.Comp.Probability);
        if (args.Component.Thrower is {} thrower)
            RaiseLocalEvent(thrower, ref ev);
        else
            thrower = EntityUid.Invalid;

        if (!SharedRandomExtensions.PredictedProb(_timing, ev.Chance, GetNetEntity(ent)))
        {
            // clients predict all thrown entities so this doesn't need to be networked
            if (_net.IsClient && _timing.IsFirstTimePredicted)
            {
                _audio.PlayLocal(ent.Comp.MissSound, ent, null);
                _popup.PopupEntity(Loc.GetString(ent.Comp.MissLocString), ent);
            }
            return;
        }

        if (!_container.Insert(args.Thrown, container))
            throw new InvalidOperationException("Container insertion failed but CanInsert returned true");

        if (_net.IsClient && _timing.IsFirstTimePredicted)
            _audio.PlayLocal(ent.Comp.InsertSound, ent, null);

        if (thrower.IsValid())
            _adminLogger.Add(LogType.Landed, LogImpact.Low, $"{ToPrettyString(args.Thrown)} thrown by {ToPrettyString(thrower):player} landed in {ToPrettyString(ent)}");
    }
}

/// <summary>
/// Raised on the thrower to let other systems modify the chance for a thrown item to be inserted.
/// </summary>
[ByRefEvent]
public record struct ModifyThrowInsertChanceEvent(float Chance);
