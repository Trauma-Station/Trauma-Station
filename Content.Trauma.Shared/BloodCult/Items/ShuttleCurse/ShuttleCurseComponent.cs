// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Items;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShuttleCurseComponent : Component
{
    [DataField]
    public TimeSpan DelayTime = TimeSpan.FromMinutes(3);

    [DataField]
    public SoundSpecifier ScatterSound = new SoundCollectionSpecifier("GlassBreak");

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CurseMessages = "BloodCultShuttleCurses";
}
