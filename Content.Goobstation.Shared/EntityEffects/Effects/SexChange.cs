// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Humanoid;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

// sleepy CROOKED joe byden's TRANS FOR EVERYONE scheme !!
// nobody makes everyone trans like i do, let me tell ya
public sealed partial class SexChange : EntityEffectBase<SexChange>
{
    /// <summary>
    ///     What sex is the target changed to? If not set then swap between male/female.
    /// </summary>
    [DataField] public Sex? NewSex;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-sex-change", ("chance", Probability));
}

public sealed class SexChangeEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, SexChange>
{
    [Dependency] private readonly SharedGoobHumanoidAppearanceSystem _goobHumanoid = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> ent, ref EntityEffectEvent<SexChange> args)
    {
        if (args.Effect.NewSex is {} sex)
        {
            _humanoid.SetSex(ent, sex, humanoid: ent.Comp);
            return;
        }

        if (ent.Comp.Sex != Sex.Unsexed)
            _goobHumanoid.SwapSex(ent, ent.Comp);
    }
}
