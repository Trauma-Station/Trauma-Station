using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

namespace Content.Trauma.Client.Heretic.SpriteOverlay;

public sealed class EntropicPlumeOverlaySystem : SpriteOverlaySystem<EntropicPlumeAffectedComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntropicPlumeAffectedComponent, AfterAutoHandleStateEvent>((uid, comp, _) =>
            AddOverlay(uid, comp));
    }
}
