// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat.RadioIconsEvents;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Trauma.Shared.Implants;

public sealed partial class ImplantRelaySystem : EntitySystem
{
    [Dependency] private SharedSubdermalImplantSystem _implant = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantedComponent, TransformSpeakerJobIconEvent>(_implant.RelayToImplantRef);
    }
}
