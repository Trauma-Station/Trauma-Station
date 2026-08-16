// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech.Components;

namespace Content.Trauma.Shared.Speech;

/// <summary>
/// Applies a single <see cref="SimpleAccentPrototype"/> to the speaking entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SimpleAccentComponent : BaseAccentComponent
{
    [DataField(required: true)]
    public ProtoId<SimpleAccentPrototype> Accent;
}
