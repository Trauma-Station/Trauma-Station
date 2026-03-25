// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Crucible.Systems;

public abstract class SharedXRayVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.XRayVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<Components.XRayVisionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<Components.XRayVisionComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || !TryComp(ent, out EyeComponent? eye))
            return;

        _eye.SetDrawFov(ent, ent.Comp.EyeHadFov, eye);
        DrawLight(true);
    }

    private void OnStartup(Entity<Components.XRayVisionComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out EyeComponent? eye))
            return;

        ent.Comp.EyeHadFov = eye.DrawFov;
        _eye.SetDrawFov(ent, false, eye);
        DrawLight(false);
    }

    protected virtual void DrawLight(bool value) { }
}
