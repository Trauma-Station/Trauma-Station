// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Viewcone.Components;

namespace Content.Trauma.Shared.Viewcone;

public sealed partial class SharedVisionSystem : EntitySystem
{
    /// <summary>
    /// calculates the input viewcone on eye position in order to check if the pos point is inside the cone it
    /// </summary>
    public bool IsVisible(Entity<ViewconeComponent> ent, Vector2 eyePos, Vector2 pos)
    {
        var dist = pos - eyePos;
        var r = ent.Comp.ConeIgnoreRadius;
        var r2 = r * r;
        if (dist.LengthSquared() < r2)
            return true; // within cone ignore radius so always visible regardless of angle

        var eyeRot = ent.Comp.ViewAngle;
        var angleDist = Math.Abs(Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot).Theta);
        return angleDist < MathHelper.DegreesToRadians(ent.Comp.CurrentConeAngle) * 0.5f;
    }
}
