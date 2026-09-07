// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Animations;

namespace Content.Goobstation.Client.Emoting;

public sealed class AnimationTrackShowSpriteLayer : AnimationTrack
{
    public List<KeyFrame> KeyFrames = new();

    public Enum? LayerKey;

    public override (int KeyFrameIndex, float FramePlayingTime) InitPlayback()
    {
        if (LayerKey == null)
        {
            throw new InvalidOperationException("Must set LayerKey.");
        }

        return (-1, 0);
    }

    public override (int KeyFrameIndex, float FramePlayingTime)
        AdvancePlayback(object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
    {
        DebugTools.AssertNotNull(LayerKey);

        var entMan = IoCManager.Resolve<IEntityManager>();

        var entity = (EntityUid) context;
        var sprite = entMan.GetComponent<SpriteComponent>(entity);

        var playingTime = prevPlayingTime + frameTime;
        var keyFrameIndex = prevKeyFrameIndex;

        while (keyFrameIndex != KeyFrames.Count - 1 && KeyFrames[keyFrameIndex + 1].KeyTime < playingTime)
        {
            playingTime -= KeyFrames[keyFrameIndex + 1].KeyTime;
            keyFrameIndex += 1;
        }

        if (keyFrameIndex >= 0)
        {
            var keyFrame = KeyFrames[keyFrameIndex];
            var spriteSys = entMan.System<SpriteSystem>();
            if (spriteSys.TryGetLayer((entity, sprite), LayerKey, out var layer, false))
            {
                spriteSys.LayerSetVisible(layer, keyFrame.Visible);
                spriteSys.LayerSetAnimationTime(layer, playingTime);
            }
        }

        return (keyFrameIndex, playingTime);
    }

    public struct KeyFrame(bool visible, float keyTime)
    {
        public readonly bool Visible = visible;
        public readonly float KeyTime = keyTime;
    }
}
