// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;

namespace Content.Trauma.Shared.Temperature;

public sealed partial class BlackBodySystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private EntityQuery<TemperatureComponent> _tempQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlackBodyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlackBodyComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
    }

    private void OnStartup(Entity<BlackBodyComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<AppearanceComponent>(ent); // fails tests if you forget this in your prototype
        if (_tempQuery.TryComp(ent, out var temp))
            _appearance.SetData(ent.Owner, BlackBodyVisuals.Temperature, temp.CurrentTemperature);
    }

    private void OnTemperatureChange(Entity<BlackBodyComponent> ent, ref OnTemperatureChangeEvent args)
    {
        _appearance.SetData(ent.Owner, BlackBodyVisuals.Temperature, args.CurrentTemperature);
    }
}
