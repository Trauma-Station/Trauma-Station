// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.CombatMode;
using Content.Trauma.Common.Vampires;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Client.Vampires;

/// <summary>
/// This handles changing the combat overlay cursor to the vampire's one.
/// </summary>
public sealed partial class BloodsuckingCursorSystem : EntitySystem
{
    [Dependency] private VampireBloodsuckingSystem _bloodsucking = default!;

    private static readonly SpriteSpecifier BloodsuckingCursor =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Trauma/Interface/Misc/crosshair_pointers.rsi"), "bloodsuck");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireBloodsuckingComponent, GetCombatModeCursorEvent>(OnCursor);
    }

    private void OnCursor(Entity<VampireBloodsuckingComponent> ent, ref GetCombatModeCursorEvent args)
    {
        if (_bloodsucking.CanBloodSuck(ent))
            args.Sprite = BloodsuckingCursor;
    }
}
