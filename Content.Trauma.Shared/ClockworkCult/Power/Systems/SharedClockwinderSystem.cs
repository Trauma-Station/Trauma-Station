// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Trauma.Shared.ClockworkCult.Power.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ClockworkCult.Power.Systems;

/// <summary>
/// The clockwinder system handles connecting clockwork structures with each other,
/// in order to power them up via updating their <see cref="LimitedChargesComponent"/>.
/// </summary>
public abstract class SharedClockwinderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockwinderComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ClockwinderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlt);
    }

    private void OnInteract(Entity<ClockwinderComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not {} target)
            return;

        // First of all, we must interact with a valid entity with ClockworkTransferrerComponent,
        // since we don't want to transfer charges from a normal structure to another normal structure.
        var ev = new ClockwinderInteractEvent(ent.Comp.Transferrer, ent.Owner);
        RaiseLocalEvent(target, ref ev);
    }

    private void OnGetAlt(Entity<ClockwinderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        // TODO: Check if user is cultist
        if (!args.CanInteract)
            return;

        // Grants access to overlay to see the connected machines
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = "Gain Vision",
            Act = () =>
            {
                if (!_timing.IsFirstTimePredicted)
                    return;

                ToggleOverlay();
            }
        });
    }

    #region Public Api

    /// <summary>
    /// Sets the current transferrer of the clockwinder. When you click an entity with <see cref="ClockworkStructureComponent"/>,
    /// they will get charges from this transferrer.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="transferrer"></param>
    public void SetTransferrer(Entity<ClockwinderComponent?> ent, EntityUid transferrer)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Transferrer = transferrer;
        Dirty(ent);
    }

    #endregion

    /// <summary>
    ///  Toggles the <see cref="ClockworkTransferOverlay"/> for the client.
    /// </summary>
    public virtual void ToggleOverlay() {}
}

/// <summary>
/// Raised when a clockwinder interacts with an entity.
/// </summary>
[ByRefEvent]
public record struct ClockwinderInteractEvent(
    EntityUid? Transferrer,
    EntityUid Clockwinder,
    bool Handled = false);
