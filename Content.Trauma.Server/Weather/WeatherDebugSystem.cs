// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Weather;

// TODO: delete this after it's fixed
public sealed class WeatherDebugSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeatherDebugComponent, MoveEvent>(OnMove);
    }

    private void OnMove(Entity<WeatherDebugComponent> ent, ref MoveEvent args)
    {
#if !DEBUG
        Log.Error($"Weather {ToPrettyString(ent)} moved from {args.OldPosition} to {args.NewPosition}!\nParent: {ToPrettyString(args.Entity.Comp1.ParentUid)}, parent changed: {args.ParentChanged}\nStack trace: {Environment.StackTrace}");
#endif
    }
}

[RegisterComponent]
public sealed partial class WeatherDebugComponent : Component;
