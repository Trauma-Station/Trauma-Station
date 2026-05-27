// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.CollectiveMind;

namespace Content.Goobstation.Server.Implants.Components;

[RegisterComponent]
public sealed partial class ImplantGrantCollectiveMindComponent : Component
{
    [DataField]
    public ProtoId<CollectiveMindPrototype> CollectiveMind;
}
