// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech.Components;

namespace Content.Trauma.Server.Wizard.Accents;

public abstract partial class AnimalAccentComponent : BaseAccentComponent
{
    [DataField]
    public virtual List<LocId> AnimalNoises { get; set; } = new();

    [DataField]
    public virtual List<LocId> AnimalAltNoises { get; set; } = new();

    [DataField]
    public virtual float AltNoiseProbability { get; set; }
}
