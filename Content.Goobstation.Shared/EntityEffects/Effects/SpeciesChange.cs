// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.Polymorph;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

public sealed partial class SpeciesChange : EntityEffectBase<SpeciesChange>
{
    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> NewSpecies;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-change-species", ("species", prototype.Index(NewSpecies).Name));
}

public abstract class SharedSpeciesChangeEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, SpeciesChange>
{
    protected override void Effect(Entity<HumanoidAppearanceComponent> ent, ref EntityEffectEvent<SpeciesChange> args)
    {
        Polymorph(ent, args.Effect.NewSpecies);
    }

    public virtual void Polymorph(EntityUid target, ProtoId<SpeciesPrototype> id)
    {
        // this 1 thing is in shared so both species effects can stay in shared, only 1 has to have a server version
    }
}
