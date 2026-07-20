// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Random;

namespace Content.Trauma.Shared.Wizard.HighFrequencyBlade;

public sealed partial class RandomRotationSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomRotationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomRotationComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsServer || IsClientSide(ent))
            _transform.SetLocalRotation(ent, _random.NextAngle());
    }
}
