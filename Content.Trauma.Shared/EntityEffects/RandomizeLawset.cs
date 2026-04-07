// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Silicons.Laws;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Entity effect that changes the lawset of the target entity to a random one from a list.
/// </summary>
public sealed partial class RandomizeLawset : EntityEffectBase<RandomizeLawset>
{
    [DataField(required: true)]
    public List<ProtoId<SiliconLawsetPrototype>> Lawsets = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}
