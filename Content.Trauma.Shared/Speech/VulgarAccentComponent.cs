// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;

namespace Content.Trauma.Shared.Speech;

[RegisterComponent, NetworkedComponent]
public sealed partial class VulgarAccentComponent : Component
{
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> Pack = "SwearWords";

    [DataField]
    public float SwearProb = 0.5f;
}
