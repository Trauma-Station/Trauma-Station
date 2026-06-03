// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Components;
using Content.Trauma.Common.Sprite;

namespace Content.Goobstation.Client.Wraith;

public sealed partial class FadingInSystem : EntitySystem
{
    [Dependency] private CommonSpriteVisibilitySystem _spriteVis = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FadingInComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, FadingInComponent fading, ComponentStartup args)
    {
        // Start fully transparent
        _spriteVis.UpdateVisibilityModifiers(uid, nameof(FadingInComponent), 0f);
        fading.Elapsed = 0f;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<FadingInComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out var fading, out _))
        {
            if (fading.Finished)
                continue;

            fading.Elapsed += frameTime;

            var alpha = Math.Clamp(fading.Elapsed / fading.FadeInTime, 0f, 1f);
            _spriteVis.UpdateVisibilityModifiers(uid, nameof(FadingInComponent), alpha);
        }
    }
}
