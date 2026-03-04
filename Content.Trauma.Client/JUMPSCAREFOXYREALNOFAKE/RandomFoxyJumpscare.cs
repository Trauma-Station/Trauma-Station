// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.MisandryBox.JumpScare;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.JUMPSCAREFOXYREALNOFAKE;

public sealed class RandomFoxyJumpscare : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IFullScreenImageJumpscare _jumpscare = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private float _timer;
    private const float Interval = 1f;
    private const string FoxyImage = "/Textures/_Trauma/NIGHTMARENIGHTMARENIGHTMARE/foxy.png";

    private SoundPathSpecifier jumpscaresound = new SoundPathSpecifier("/Audio/_Trauma/fnaf/fnaf-2-death-scream.ogg");

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _timer += frameTime;

        if (_timer < Interval)
            return;

        _timer -= Interval;

        if (_random.Prob(1f / 10000f))
            DoJumpscare();
    }

    private void DoJumpscare()
    {
        var image = new SpriteSpecifier.Texture(new ResPath(FoxyImage));

        // JUMPSCARE EVERYONE AT THE SAME TIME
        foreach (var session in _player.Sessions)
        {
            _audio.PlayGlobal(jumpscaresound, session);
            _jumpscare.Jumpscare(image, session);
        }
    }
}
