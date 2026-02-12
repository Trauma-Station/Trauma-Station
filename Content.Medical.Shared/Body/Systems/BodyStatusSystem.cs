// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Wounds;
using Content.Shared.Mobs;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Body;

public sealed class BodyStatusSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyStatusComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BodyStatusComponent, MobStateChangedEvent>(OnMobStateChange);
    }

    private void OnMapInit(Entity<BodyStatusComponent> ent, ref MapInitEvent args)
    {
        UpdateStatus(ent.AsNullable());
    }

    private void OnMobStateChange(Entity<BodyStatusComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateStatus(ent.AsNullable());
    }

    public void UpdateStatus(Entity<BodyStatusComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.BodyStatus = _wound.GetWoundableStatesOnBodyPainFeels(ent.Owner);
        Dirty(ent, ent.Comp);

        var ev = new TargetIntegrityChangedMessage();
        if (_net.IsClient)
            RaiseLocalEvent(ev);
        else
            RaiseNetworkEvent(ev, ent);
    }
}

/// <summary>
/// Message sent by the server/predicted when a body's parts get damaged, to update the part status UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class TargetIntegrityChangedMessage: EntityEventArgs;
