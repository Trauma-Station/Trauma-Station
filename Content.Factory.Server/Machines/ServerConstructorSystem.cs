// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Machines;
using Content.Server.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.DoAfter;

namespace Content.Factory.Server.Machines;

public sealed partial class ServerConstructorSystem : ConstructorSystem
{
    [Dependency] private ConstructionSystem _construction = default!;
    [Dependency] private StartableMachineSystem _machine = default!;
    [Dependency] private EntityQuery<ActiveDoAfterComponent> _activeQuery = default!;

    [SubscribeLocalEvent]
    private void OnStarted(Entity<ConstructorComponent> ent, ref MachineStartedEvent args)
    {
        // can't start if it's already building something
        if (_activeQuery.HasComp(ent))
            _machine.Failed(ent.Owner);
        else
            Construct(ent);
    }

    // async because construction shitcode
    private async void Construct(Entity<ConstructorComponent> ent)
    {
        var uid = ent.Owner;
        if (ent.Comp.Construction is not {} id)
        {
            _machine.Failed(uid);
            return;
        }

        _machine.Started(uid);

        var proto = ProtoMan.Index(id);
        var completed = proto.Type switch
        {
            ConstructionType.Structure => await _construction.TryStartStructureConstruction(uid, id, OutputPosition(ent), Angle.Zero),
            ConstructionType.Item => await _construction.TryStartItemConstruction(id, uid),
            _ => false
        };

        if (completed)
            _machine.Completed(uid);
        else
            _machine.Failed(uid);
    }
}
