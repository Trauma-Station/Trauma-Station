// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Popups;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed partial class CoinFlipSystem : SharedCoinFlipSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoinFlipComponent, MapInitEvent>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<CoinFlipComponent>();
        while (query.MoveNext(out var uid, out var coin))
        {
            if (!coin.IsFlipping)
                continue;

            if (now < coin.FlipEndTime)
                continue;

            coin.IsFlipping = false;
            coin.CurrentSide = _random.Pick(coin.Sides);

            _popup.PopupEntity(Loc.GetString("coin-flip-popup-message",
                    ("coin", uid),
                    ("side", Loc.GetString(coin.CurrentSide.Name))),
                uid);
            Appearance.SetData(uid, CoinFlipVisuals.SpriteState, coin.CurrentSide.SpriteState);
            if (coin.User is { } user)
                _effects.ApplyEffects(user, coin.CurrentSide.UserEffects);
            coin.User = null;

            Dirty(uid, coin);
        }
    }

    private void OnInit(Entity<CoinFlipComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Sides.Count == 0)
        {
            Log.Error($"{ToPrettyString(ent)} has 0 coin sides");
            QueueDel(ent);
            return;
        }

        ent.Comp.IsFlipping = false;
        ent.Comp.CurrentSide ??= _random.Pick(ent.Comp.Sides);
        Appearance.SetData(ent, CoinFlipVisuals.SpriteState, ent.Comp.CurrentSide.SpriteState);
        Dirty(ent);
    }
}
