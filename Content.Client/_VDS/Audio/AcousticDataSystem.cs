// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
// SPDX-FileCopyrightText: 2025 ark1368
// SPDX-FileCopyrightText: 2025 Jellvisk
//
// SPDX-License-Identifier: MPL-2.0

// this has been heavily refactored by Jellvisk to the point
// where this is like a ship of theseus situation.

using Content.Client._Mono.Audio;
using Content.Client._VDS.Audio.Components;
using Content.Shared.Coordinates;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared._VDS.Audio.Components;
using Content.Shared._VDS.CCVars;
using Content.Shared._VDS.Physics;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;

namespace Content.Client._VDS.Audio;

/// <summary>
/// Gathers environmental acoustic data around the player, later to be processed by <see cref="AudioEffectSystem"/>.
/// </summary>
public sealed class AcousticDataSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectSystem _audioEffectSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReflectiveRaycastSystem _reflectiveRaycast = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roofSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;

    /// <summary>
    /// Quick reference to our <see cref="AcousticSettingsComponent"/>
    /// </summary>
    private AcousticSettingsComponent _acousticSettings = default!;

    /// <summary>
    /// The directions that are raycasted.
    /// Used relative to the grid.
    /// </summary>
    private Angle[] _calculatedDirections = [Direction.North.ToAngle(), Direction.West.ToAngle(), Direction.South.ToAngle(), Direction.East.ToAngle()];

    /*  - VDS
        TODO: this could be expanded to be more than just these few presets. see ReverbPresets.cs in Robust.Shared/Audio/Effects/
        would require gathering more data.
    */
    /// <summary>
    /// Arbitrary values for determining what ReverbPreset to use.
    /// Defined in <see cref="AcousticSettingsComponent"/>.
    /// See <see cref="Robust.Shared.Audio.Effects.ReverbPresets"/>.
    /// </summary>
    private SortedList<float, ProtoId<AudioPresetPrototype>>? _acousticPresets;

    /// <summary>
    /// The client's local entity, to spawn our raycasts at.
    /// </summary>
    private EntityUid _clientEnt;

    private bool _acousticEnabled = true;

    /// <summary>
    /// Max amount of times single acoustic ray is allowed to bounce
    /// </summary>
    private int _acousticMaxReflections;

    /// <summary>
    /// Our previously recorded magnitude, for lerp purposes.
    /// </summary>
    private float _prevAvgMagnitude;


    private EntityQuery<AcousticDataComponent> _acousticQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<RoofComponent> _roofQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logMan.GetSawmill("acoustics");

        _configurationManager.OnValueChanged(VCCVars.AcousticEnable, x => _acousticEnabled = x, invokeImmediately: true);
        _configurationManager.OnValueChanged(VCCVars.AcousticHighResolution, x => _calculatedDirections = GetEffectiveDirections(x), invokeImmediately: true);
        _configurationManager.OnValueChanged(VCCVars.AcousticReflectionCount, x => _acousticMaxReflections = x, invokeImmediately: true);

        _acousticQuery = GetEntityQuery<AcousticDataComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _roofQuery = GetEntityQuery<RoofComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        /*
           this is kinda janky as fuck. it also wasn't me who originally did it i swear
           but to be fair it works good enough and I can't think of any other solution right
           now and i'm tired good night
        */
        SubscribeLocalEvent<AudioComponent, EntParentChangedMessage>(OnParentChange);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        _clientEnt = ev.Entity;
        EnsureComp<AcousticSettingsComponent>(_clientEnt, out var comp);
        _acousticPresets = comp.ReverbPresets;
        _acousticSettings = comp;
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        RemComp<AcousticSettingsComponent>(_clientEnt);
        _clientEnt = EntityUid.Invalid;
    }

    private void OnParentChange(Entity<AudioComponent> audio, ref EntParentChangedMessage ev)
    {
        if (!CanAudioBePostProcessed(audio, ev.Transform))
            return;

        ProcessAcoustics(audio);
    }

    /// <summary>
    /// Cast, get, and process the obtained <see cref="AcousticRayResults"/>.
    /// </summary>
    private void ProcessAcoustics(Entity<AudioComponent> audioEnt)
    {
        if (_acousticPresets == null || _acousticPresets.Count == 0)
            return;

        var maxMagnitude = _acousticPresets.Keys[^1];
        var minMagnitude = _acousticPresets.Keys[0];

        var magnitude = 0f;
        if (TryCastAndGetEnvironmentAcousticData(
            in _clientEnt,
            in maxMagnitude,
            in _acousticMaxReflections,
            in _calculatedDirections,
            out var acousticResults))
        {
            magnitude = CalculateAmplitude(
                (_clientEnt, Transform(_clientEnt)),
                in acousticResults);
        }

        if (magnitude > minMagnitude)
        {
            var bestPreset = GetBestReverbPreset(magnitude, _acousticPresets);
            _audioEffectSystem.TryAddEffect(in audioEnt, in bestPreset);
        }
        else
        {
            _audioEffectSystem.TryRemoveEffect(in audioEnt);
        }
    }

    /// <summary>
    /// Basic check for whether an audio entity can be applied effects such as reverb.
    /// </summary>
    public bool CanAudioBePostProcessed(Entity<AudioComponent> audio, in TransformComponent xForm)
    {
        if (!_acousticEnabled)
            return false;

        // we cast from the player, so they need a valid entity.
        if (!_clientEnt.IsValid())
            return false;

        if (TerminatingOrDeleted(audio))
            return false;

        //  we only care about loaded local audio. it would be kinda weird
        //  if stuff like nukie music reverbed
        if (!audio.Comp.Playing
            || audio.Comp.Global
            || audio.Comp.State == AudioState.Stopped)
        {
            return false;
        }

        Vector2 audioPos;
        Vector2 clientPos;
        if ((audio.Comp.Flags & AudioFlags.GridAudio) != 0x0)
        {
            audioPos = xForm.LocalPosition;
            clientPos = _mapSystem.GetGridPosition(_clientEnt);
        }
        else
        {
            audioPos = _transformSystem.GetWorldPosition(xForm);
            clientPos = _transformSystem.GetWorldPosition(_clientEnt);
        }

        // check distance!
        var delta = audioPos - clientPos;
        var distance = delta.Length();
        return _audioSystem.GetAudioDistance(distance) <= audio.Comp.MaxDistance;
    }

    /// <summary>
    /// Compares our magnitude to <see cref="AcousticReverbPresets"/> and returns the best match.
    /// </summary>
    [Pure]
    public static ProtoId<AudioPresetPrototype> GetBestReverbPreset(float magnitude, SortedList<float, ProtoId<AudioPresetPrototype>> presetList)
    {
        var keys = presetList.Keys;
        var index = keys.ToList().BinarySearch(magnitude);

        // our magnitude was found exactly in the list so just take it i guess.
        if (index >= 0)
            return presetList.GetValueAtIndex(index);

        // invert the bits to get our insertion point
        index = ~index;
        var lowerIndex = index - 1;
        var upperIndex = index;

        // edge cases
        if (upperIndex == 0) // magnitude is smaller than the first element of our list
            return presetList.GetValueAtIndex(upperIndex);
        else if (lowerIndex == presetList.Count - 1) // magnitude is bigger than the last element of our list
            return presetList.GetValueAtIndex(lowerIndex);

        // return the value of whatever is closest to our magnitude
        var lowerDiff = MathF.Abs(magnitude - keys[lowerIndex]);
        var upperDiff = MathF.Abs(magnitude - keys[upperIndex]);
        return (lowerDiff <= upperDiff) ? presetList.GetValueAtIndex(lowerIndex) : presetList.GetValueAtIndex(upperIndex);
    }

    /// <summary>
    /// Returns all four cardinal directions when <paramref name="highResolution"/> is false.
    /// Otherwise, returns all eight intercardinal and cardinal directions as listed in
    /// <see cref="DirectionExtensions.AllDirections"/>.
    /// </summary>
    [Pure]
    public static Angle[] GetEffectiveDirections(bool highResolution)
    {
        if (highResolution)
        {
            var allDirections = DirectionExtensions.AllDirections;
            var directions = new Angle[allDirections.Length];

            for (var i = 0; i < allDirections.Length; i++)
                directions[i] = allDirections[i].ToAngle();

            return directions;
        }

        return [Direction.North.ToAngle(), Direction.West.ToAngle(), Direction.South.ToAngle(), Direction.East.ToAngle()];
    }

    /// <summary>
    /// Attempts to cast and gather environmental <see cref="AcousticRayResults"/> around <paramref name="originEnt"/>.
    /// <seealso cref="ReflectiveRaycastSystem"/>
    /// </summary>
    /// <param name="originEnt">The origin of our raycasts.</param>
    /// <param name="maxRange">Maximum range of a ray.</param>
    /// <param name="maxBounces">How many times a ray is allowed to bounce before terminating early.</param>
    /// <param name="castDirections">What angles our rays will shoot out from.</param>
    /// <param name="acousticResults">A list of <see cref="AcousticRayResults"/>.</param>
    /// <returns>True if <paramref name="acousticResults"/> has data, false if <paramref name="acousticResults"/> is null or empty.</returns>
    public bool TryCastAndGetEnvironmentAcousticData(
        in EntityUid originEnt,
        in float maxRange,
        in int maxBounces,
        in Angle[] castDirections,
        [NotNullWhen(true)] out List<AcousticRayResults>? acousticResults)
    {
        acousticResults = new List<AcousticRayResults>(castDirections.Length);

        if (!originEnt.IsValid()
            || !_transformQuery.HasComponent(originEnt))
        {
            return false;
        }

        // in space nobody can hear your awesome freaking acoustics
        if (!_turfSystem.TryGetTileRef(originEnt.ToCoordinates(), out var tileRef)
            || _turfSystem.IsSpace(tileRef.Value))
        {
            return false;
        }

        var clientTransform = Transform(originEnt);
        var clientMapId = clientTransform.MapID;
        var clientCoords = _transformSystem.ToMapCoordinates(clientTransform.Coordinates).Position;

        // our path filter, which will return AcousticDataComponent entities our ray passes through
        var pathFilter = new QueryFilter
        {
            MaskBits = (int)CollisionGroup.AllMask,
            IsIgnored = ent => !_acousticQuery.HasComp(ent), // ideally we'd pass _absorptionQuery via state, but the new ray system doesn't allow that for some reason
            Flags = QueryFlags.Static | QueryFlags.Dynamic
        };

        // our probe filter, which determines what our rays will bounce off of.
        var probeFilter = new QueryFilter
        {
            MaskBits = (int)CollisionGroup.AllMask,
            LayerBits = (int)CollisionGroup.None,
            IsIgnored = ent => _acousticQuery.TryGetComponent(ent, out var comp) && !comp.ReflectRay,
            Flags = QueryFlags.Static | QueryFlags.Dynamic
        };

        // our current ray state, which is passed through and altered by ref.
        // instead of making states for each ray we will just reuse one and reset it
        // before passing it back in for the next direction. for performance or whatever.
        var state = new ReflectiveRayState(
                probeFilter,
                pathFilter,
                origin: clientCoords,
                direction: Vector2.Zero, // we change the dir later
                maxRange: maxRange,
                clientMapId
                );

        // cast our rays and get our results
        acousticResults = CastManyReflectiveAcousticRays(
            in originEnt,
            clientCoords,
            in maxBounces,
            in castDirections,
            ref state);

        return acousticResults.Count != 0;
    }

    /// <summary>
    /// Casts many bouncing rays.
    /// <seealso cref="ReflectiveRaycastSystem"/>
    /// <seealso cref="CastReflectiveAcousticRay(in EntityUid, in int, ref ReflectiveRayState)"/>
    /// </summary>
    public List<AcousticRayResults> CastManyReflectiveAcousticRays(
        in EntityUid originEnt,
        Vector2 originCoords,
        in int maxBounces,
        in Angle[] castDirections,
        ref ReflectiveRayState state)
    {
        var acousticResults = new List<AcousticRayResults>();

        foreach (var direction in castDirections)
        {
            state.CurrentPos = originCoords;
            state.OldPos = originCoords;
            state.Direction = (direction + _random.NextFloat(
                -_acousticSettings.DirectionRandomOffset,
                _acousticSettings.DirectionRandomOffset)).ToVec();
            state.Translation = state.Direction * state.MaxRange;
            state.ProbeTranslation = state.Translation;
            state.RemainingDistance = state.MaxRange;


            // handle individual bounces
            var results = CastReflectiveAcousticRay(
                in originEnt,
                in maxBounces,
                ref state);
            acousticResults.Add(results);
        }

        return acousticResults;
    }

    /// <summary>
    /// Casts a bouncing ray.
    /// <seealso cref="ReflectiveRaycastSystem"/>
    /// </summary>
    /// <param name="originEnt">The entity to compare absorption falloff to. <see cref="GetAcousticAbsorption(RayHit,
    /// in EntityUid, in float, in AcousticDataComponent)/></param>
    /// <param name="state"><see cref="ReflectiveRayState"/></param>
    /// <returns><see cref="AcousticRayResults"/>, in order hit, including whatever the ray bounced off.</returns>
    public AcousticRayResults CastReflectiveAcousticRay(
        in EntityUid originEnt,
        in int maxBounces,
        ref ReflectiveRayState state)
    {
        var results = new AcousticRayResults();
        for (var bounce = 0; bounce <= maxBounces; bounce++)
        {
            /*
                our raycast state will constantly be fed by reference into the reflective raycast API,
                which updates the reference's positional data for us, including the handling of
                bounces with each iteration (provided we pass the ref back through).
                we also get a new list of entities for each iteration so we
                can do component data gathering on them.
            */
            var (probeResult, pathResults) = _reflectiveRaycast.CastAndUpdateReflectiveRayStateRef(ref state);

            results.TotalRange += state.CurrentSegmentDistance;
            if (probeResult.Hit)
            {
                pathResults.Results.Add(probeResult.Results[0]); // we wanna include what we hit to our data too
                results.TotalBounces++;
            }

            // gather acoustic component data
            if (pathResults.Results.Count > 0)
            {
                foreach (var result in pathResults.Results)
                {
                    if (!_acousticQuery.TryGetComponent(result.Entity, out var comp))
                        continue;

                    // TODO: more component data can be gathered here in the future
                    results.TotalAbsorption += GetAcousticAbsorption(
                        result,
                        in originEnt,
                        in comp);
                }
            }

            // this ray is long enough to be considered in an open area and now shall be ignored
            if (state.CurrentSegmentDistance >= state.MaxRange * _acousticSettings.EscapeDistancePercentage)
            {
                results.TotalEscapes++;
                break;
            }

            // expended our range budget, break the loop
            if (results.TotalRange >= state.MaxRange)
                break;
        }
        return results;
    }

    /// <summary>
    /// Gets an absorption percentage using inverse square falloff.
    /// </summary>
    private float GetAcousticAbsorption(
        RayHit result,
        in EntityUid originEnt,
        in AcousticDataComponent comp)
    {
        result.Entity.ToCoordinates().TryDistance(
            EntityManager,
            originEnt.ToCoordinates(),
            out var distance);

        // make sure we don't divide by zero.
        var distanceSquared = MathF.Max(distance * distance, 0.01f);

        return (comp.Absorption < 0)
            ? -NormalizeToPercentage(comp.Absorption, -100f, 0f, maxClamp: _acousticSettings.MaxAbsorptionClamp) * distanceSquared
            : NormalizeToPercentage(comp.Absorption, maxClamp: _acousticSettings.MaxAbsorptionClamp) * distanceSquared;
    }


    /// <summary>
    /// Calculates our the overall amplitude of <paramref name="acousticResults"/>.
    /// </summary>
    /// <param name="originEnt">Where the rays originally came from, for roof detecting purposes.</param>
    /// <returns>Our ray's amplitude</returns>
    private float CalculateAmplitude(
        Entity<TransformComponent> originEnt,
        in List<AcousticRayResults> acousticResults)
    {
        var totalRays = acousticResults.Count;
        var avgMagnitude = acousticResults.Average(mag => mag.TotalRange);
        var avgAbsorption = acousticResults.Average(absorb => absorb.TotalAbsorption);
        var escaped = acousticResults.Sum(escapees => escapees.TotalEscapes);
        // TODO: resonance??
        // var avgBounces = (float)acousticResults.Average(bounce => bounce.TotalBounces);

        // we store our previous avg magnitude and lerp it with the current to make sure changes aren't too jarring
        if (_prevAvgMagnitude > float.Epsilon)
            avgMagnitude = MathHelper.Lerp(_prevAvgMagnitude, avgMagnitude, _acousticSettings.AvgMagnitudeBlend);
        _prevAvgMagnitude = avgMagnitude;

        var amplitude = 0f;
        var absorbMultiplier = InverseNormalizeToPercentage(avgAbsorption, maxClamp: _acousticSettings.MaxAbsorptionClamp); // things like furniture or different material walls should eat our energy
        var escapeMultiplier = MathF.Max(InverseNormalizeToPercentage(escaped, 0f, totalRays), _acousticSettings.MaxmimumEscapePenalty); // escaped rays are mostly irrelevant, so penalize based on that.

        amplitude += avgMagnitude;
        amplitude *= absorbMultiplier;
        amplitude *= escapeMultiplier;

        // severely punish our amplitude if there is no roof.
        if (originEnt.Comp.GridUid.HasValue
            && _roofQuery.TryGetComponent(originEnt.Comp.GridUid.Value, out var roof)
            && _gridQuery.TryGetComponent(originEnt.Comp.GridUid.Value, out var grid)
            && _transformSystem.TryGetGridTilePosition(originEnt.Owner, out var indices)
            && !_roofSystem.IsRooved((originEnt.Comp.GridUid.Value, grid, roof), indices))
        {
            amplitude *= _acousticSettings.NoRoofPenalty;
        }

        // _sawmill.Debug($"""
        //         Results:
        //         Absorbtion Multiplier: {absorbMultiplier:F3}
        //         Escape Multiplier: {escapeMultiplier:F3}
        //         Final Amplitude: {amplitude:F3}
        //         Acoustic Preset: {GetBestReverbPreset(amplitude, _acousticPresets!)}
        //         """);

        return amplitude;
    }

    /// <summary>
    /// Returns a 0f..1f percent, where the closer to 0f the value is, the closer to 100% (1.0f) it is.
    /// </summary>
    public static float NormalizeToPercentage(
        float value,
        float minValue = 0f,
        float maxValue = 100f,
        float maxClamp = 1f)
    {
        var percentage = (value - minValue) / (maxValue - minValue);
        return Math.Clamp(percentage, 0f, maxClamp);
    }

    /// <summary>
    /// Returns a 0f..1f percent, where the closer to 1.0f the value is, the closer to 0% (0f) it is.
    /// </summary>
    public static float InverseNormalizeToPercentage(
        float value,
        float minValue = 0f,
        float maxValue = 100f,
        float maxClamp = 1f)
    {
        return NormalizeToPercentage(maxValue - value, minValue, maxValue, maxClamp);
    }

    /// <summary>
    /// Data about the current acoustic environment and relevant variables.
    /// </summary>
    public struct AcousticRayResults
    {
        public float TotalAbsorption;
        public int TotalBounces;
        public int TotalEscapes;
        public float TotalRange;
    }
}
