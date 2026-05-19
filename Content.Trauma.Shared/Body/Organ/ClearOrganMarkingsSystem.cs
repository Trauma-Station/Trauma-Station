// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;

namespace Content.Trauma.Shared.Body.Organ;

public sealed partial class ClearOrganMarkingsSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ClearOrganMarkingsEvent>(_body.RefRelayBodyEvent);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, BodyRelayedEvent<ClearOrganMarkingsEvent>>(OnClear);
    }

    private void OnClear(Entity<VisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<ClearOrganMarkingsEvent> args)
    {
        _visualBody.SetOrganMarkings(ent, new());
    }
}
