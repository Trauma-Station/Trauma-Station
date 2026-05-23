// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Radar;

public abstract class CommonRadarBlipsSystem : EntitySystem
{
    public abstract void RequestBlips(EntityUid console);

    /// <summary>
    /// Gets the raw blips data which includes grid information for more accurate rendering.
    /// </summary>
    public abstract List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> GetRawBlips();

    /// <summary>
    /// Gets the raw hitscan data which includes grid information for more accurate rendering.
    /// </summary>
    public abstract List<(NetEntity? Grid, Vector2 Start, Vector2 End, float Thickness, Color Color)> GetRawHitscanLines();
}
