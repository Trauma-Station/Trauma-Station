// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

[CVarDefs]
public sealed partial class TraumaCVars
{
    #region Disabling features

    /// <summary>
    /// Whether to disable pathfinding, used for tests to not balloon memory usage and runtime.
    /// </summary>
    public static readonly CVarDef<bool> DisablePathfinding =
        CVarDef.Create("trauma.disable_pathfinding", false, CVar.SERVER);

    #endregion

    #region AudioMuffle

    /// <summary>
    /// Is audio muffle pathfinding behavior enabled?
    /// </summary>
    public static readonly CVarDef<bool> AudioMufflePathfinding =
        CVarDef.Create("trauma.audio_muffle_pathfinding", true, CVar.SERVER | CVar.REPLICATED);

    #endregion

    #region Streamer Mode

    /// <summary>
    /// Client setting to disable music that would cause copyright claims.
    /// </summary>
    public static readonly CVarDef<bool> StreamerMode =
        CVarDef.Create("trauma.streamer_mode", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    #endregion

    #region Gun prediction

    /// <summary>
    /// Distance used between projectile and lag-compensated target position for gun prediction.
    /// </summary>
    public static readonly CVarDef<float> GunLagCompRange =
        CVarDef.Create("trauma.gun_lag_comp_range", 0.6f, CVar.SERVER);

    #endregion

    #region Softcrit

    /// <summary>
    /// Speed modifier for softcrit mobs, on top of being forced to crawl.
    /// </summary>
    public static readonly CVarDef<float> SoftCritMoveSpeed =
        CVarDef.Create("trauma.softcrit_move_speed", 0.5f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Inhaled gas modifier for softcrit mobs, makes it harder to breathe.
    /// This means you can't just crawl around forever if you aren't bleeding out.
    /// </summary>
    public static readonly CVarDef<float> SoftCritInhaleModifier =
        CVarDef.Create("trauma.softcrit_inhale_modifier", 0.3f, CVar.SERVER | CVar.REPLICATED);

    #endregion

    #region Skills

    /// <summary>
    /// Enables gaining XP and skills during rounds.
    /// Character starting skills are not affected by this.
    /// </summary>
    public static readonly CVarDef<bool> SkillGain =
        CVarDef.Create("trauma.skill_gain", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Client setting to hide all skill-related popups.
    /// </summary>
    public static readonly CVarDef<bool> SkillPopups =
        CVarDef.Create("trauma.skill_popups", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    #endregion

    #region Chat

    /// <summary>
    /// Whether to play a sound when a highlighted message is received.
    /// </summary>
    public static readonly CVarDef<bool> ChatHighlightSound =
        CVarDef.Create("chat.highlight_sound", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Volume of the highlight sound when a highlighted message is received.
    /// </summary>
    public static readonly CVarDef<float> ChatHighlightVolume =
        CVarDef.Create("chat.highlight_volume", 1f, CVar.ARCHIVE | CVar.CLIENTONLY);

    #endregion

    #region Webhooks

    /// <summary>
    /// Discord webhook to send errors to.
    /// Disabled if this is empty.
    /// </summary>
    public static readonly CVarDef<string> ErrorWebhookUrl =
        CVarDef.Create("trauma.error_webhook_url", string.Empty, CVar.SERVER);

    /// <summary>
    /// Delay between each error message in seconds.
    /// Used to avoid hitting ratelimits
    /// </summary>
    public static readonly CVarDef<double> ErrorWebhookDelay =
        CVarDef.Create("trauma.error_webhook_delay", 0.3, CVar.SERVER);

    #endregion

    #region EndCredits

    /// <summary>
    /// Whether to play the cool end credits.
    /// </summary>
    public static readonly CVarDef<bool> PlayMovieEndCredits =
        CVarDef.Create("trauma.play_credits", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    #endregion

        #region Audio

    /// <summary>
    ///     Whether to render sounds with echo when they are in 'large' open, rooved areas.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<bool> AreaEchoEnabled =
        CVarDef.Create("mono.area_echo.enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     If false, area echos calculate with 4 directions (NSEW).
    ///         Otherwise, area echos calculate with all 8 directions.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<bool> AreaEchoHighResolution =
        CVarDef.Create("mono.area_echo.alldirections", false, CVar.ARCHIVE | CVar.CLIENTONLY);


    /// <summary>
    ///     How many times a ray can bounce off a surface for an echo calculation.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<int> AreaEchoReflectionCount =
        CVarDef.Create("mono.area_echo.max_reflections", 1, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Distantial interval, in tiles, in the rays used to calculate the roofs of an open area for echos,
    ///         or the ray's distance to space, at which the tile at that point of the ray is processed.
    ///
    ///     The lower this is, the more 'predictable' and computationally heavy the echoes are.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<float> AreaEchoStepFidelity =
        CVarDef.Create("mono.area_echo.step_fidelity", 5f, CVar.CLIENTONLY);

    /// <summary>
    ///     Interval between updates for every audio entity.
    /// </summary>
    /// <seealso cref="AreaEchoSystem"/>
    public static readonly CVarDef<TimeSpan> AreaEchoRecalculationInterval =
        CVarDef.Create("mono.area_echo.recalculation_interval", TimeSpan.FromSeconds(15), CVar.ARCHIVE | CVar.CLIENTONLY);

        #endregion
}
