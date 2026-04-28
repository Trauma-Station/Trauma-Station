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

    /// <summary>
    /// Disables DogVision which sandevistan uses.
    /// </summary>
    public static readonly CVarDef<bool> NoVisionFilters =
        CVarDef.Create("accessibility.no_vision_filters", false, CVar.CLIENTONLY | CVar.ARCHIVE);

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

    /// <summary>
    /// You get instantly banned if you say something matching this regex in any chat channel.
    /// The regex is case insensitive.
    /// </summary>
    public static readonly CVarDef<string> GamerWordsRegex =
        CVarDef.Create("chat.gamer_words_regex", string.Empty, CVar.SERVER);

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
    public static readonly CVarDef<float> ErrorWebhookDelay =
        CVarDef.Create("trauma.error_webhook_delay", 0.3f, CVar.SERVER);

    /// <summary>
    /// How many messages can be queued at once.
    /// If this limit is exceeded the oldest messages get dropped.
    /// Changing this ingame drops all currently queued messages.
    /// </summary>
    public static readonly CVarDef<int> ErrorWebhookLimit =
        CVarDef.Create("trauma.error_webhook_limit", 64, CVar.SERVER);

    #endregion

    #region EndCredits

    /// <summary>
    /// Whether to play the cool end credits.
    /// </summary>
    public static readonly CVarDef<bool> PlayMovieEndCredits =
        CVarDef.Create("trauma.play_credits", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    #endregion

    #region Decals

    /// <summary>
    /// How long despawning decals like footprints and blood splatters last before despawning.
    /// </summary>
    public static readonly CVarDef<float> DecalDespawnTime =
        CVarDef.Create("trauma.decal_despawn_time", 300f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// How many decals are allowed to be despawning at the same time.
    /// If another decal is spawned, it will remove the oldest decal.
    /// If this value is changed ingame it will only take affected after restarting the round.
    /// </summary>
    public static readonly CVarDef<int> DecalDespawnLimit =
        CVarDef.Create("trauma.decal_despawn_limit", 128, CVar.SERVER | CVar.REPLICATED);

    #endregion

    #region Station Traits

    /// <summary>
    /// Whether to enable station traits.
    /// Used to prevent tests failing for spawned gamerules.
    /// </summary>
    public static readonly CVarDef<bool> StationTraitsEnabled =
        CVarDef.Create("trauma.station_traits_enabled", true, CVar.SERVER);

    /// <summary>
    /// The offset for in-game date (the date will be server date + this amount of years).
    /// Displayed in the station report and PDAs.
    /// </summary>
    public static readonly CVarDef<int> YearOffset =
        CVarDef.Create("game.current_year_offset", 739, CVar.SERVERONLY);

    #endregion

    #region Antag Summoner

    /// <summary>
    /// Minimum number of players for antag summoner to work, to prevent farming money when nobody is even going to take the ghost roles.
    /// </summary>
    public static readonly CVarDef<int> AntagSummonerMinPlayers =
        CVarDef.Create("trauma.antag_summoner_min_players", 30, CVar.SERVER);

    #endregion

    #region Particles

    /// <summary>
    /// Controls particle effect quality.
    /// 0 = Off, 1 = Low, 2 = Medium, 3 = High
    /// Low:    25% of maxCount per emitter
    /// Medium: 50% of maxCount per emitter
    /// High:   100% of maxCount per emitter
    ///
    /// Note: Particles with IgnoreQualitySettings = true always render at full quality.
    /// </summary>
    public static readonly CVarDef<int> ParticleQuality =
        CVarDef.Create("particles.quality", 3, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum total number of live particles allowed on screen at once across all emitters.
    /// Emitters will reduce their emission rate once the budget is exhausted.
    /// Overridden by ParticleQuality presets but can be set manually when quality is High.
    /// </summary>
    public static readonly CVarDef<int> ParticleGlobalBudget =
        CVarDef.Create("particles.global_budget", 8000, CVar.CLIENTONLY | CVar.ARCHIVE);

    #endregion
}
