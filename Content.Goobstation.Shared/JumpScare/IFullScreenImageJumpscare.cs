// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.JumpScare;

public interface IFullScreenImageJumpscare
{
    /// <summary>
    /// Sends a jumpscare to client, session being null implies it's called by client.
    /// </summary>
    public void Jumpscare(SpriteSpecifier.Texture image, ICommonSession? session = null);
}
