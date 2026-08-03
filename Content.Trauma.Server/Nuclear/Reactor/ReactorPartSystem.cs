// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Nutrition;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Content.Trauma.Shared.Nuclear;
using Content.Trauma.Shared.Nuclear.Reactor;
using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Nuclear.Reactor;

/// <remarks>
/// Inspired by https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/reactorcomponents.dm
/// </remarks>
public sealed partial class ReactorPartSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private NuclearReactorSystem _reactor = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedRadiationSystem _radiation = default!;
    [Dependency] private EntityQuery<NuclearPropertiesComponent> _propsQuery = default!;
    [Dependency] private EntityQuery<NuclearReactorComponent> _reactorQuery = default!;
    [Dependency] private EntityQuery<ReactorControlRodComponent> _controlQuery = default!;
    [Dependency] private EntityQuery<ReactorGasChannelComponent> _channelQuery = default!;

    private static readonly ProtoId<DamageTypePrototype> Heat = "Heat";
    private static readonly ProtoId<DamageTypePrototype> Radiation = "Radiation";

    /// <summary>
    /// Changes the overall rate of events
    /// </summary>
    private readonly float _rate = 5;

    /// <summary>
    /// Changes the likelyhood of neutron interactions
    /// </summary>
    private readonly float _bias = 1.5f;

    /// <summary>
    /// The amount of a property consumed by a reaction
    /// </summary>
    private readonly float _reactant = 0.01f;

    /// <summary>
    /// The amount of a property resulting from a reaction
    /// </summary>
    private readonly float _product = 0.005f;

    /// <summary>
    /// Temperature in Kelvin when people's hands can be burnt
    /// </summary>
    private readonly static float _hotTemp = Atmospherics.T0C + 80;

    /// <summary>
    /// Temperature in Kelvin when insulated gloves can no longer protect
    /// </summary>
    private readonly static float _burnTemp = Atmospherics.T0C + 400;

    private readonly static float _burnDiv = (_burnTemp - _hotTemp) / 5; // The 5 is how much heat damage insulated gloves protect from

    private readonly float _threshold = 1f;
    private float _accumulator = 0f;

    #region Item Methods
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearPropertiesComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ReactorPartComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<NuclearPropertiesComponent, IngestedEvent>(OnIngest);
    }

    private void OnMapInit(Entity<NuclearPropertiesComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        if (comp.SpentFuel > 0)
        {
            var source = EnsureComp<RadiationSourceComponent>(uid);
            _radiation.SetIntensity((uid, source), comp.SpentFuel);
        }

        if (comp.NeutronRadioactivity > 0)
        {
            var lightcomp = _light.EnsureLight(uid);
            _light.SetEnergy(uid, comp.NeutronRadioactivity, lightcomp);
            _light.SetColor(uid, Color.FromHex("#22bbff"), lightcomp);
            _light.SetRadius(uid, 1.2f, lightcomp);
        }
    }

    private void OnExamine(Entity<ReactorPartComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(ReactorPartComponent)))
        {
            if (_propsQuery.TryComp(ent, out var props))
            {
                if (props.NeutronRadioactivity > 0)
                {
                    var n = RadScale(props.NeutronRadioactivity);
                    args.PushMarkup(Loc.GetString($"reactor-part-nrad-{n}"));
                }

                if (props.Radioactivity > 0)
                {
                    var n = RadScale(props.Radioactivity);
                    args.PushMarkup(Loc.GetString($"reactor-part-rad-{n}"));
                }
            }

            if (comp.Temperature > _burnTemp)
                args.PushMarkup(Loc.GetString("reactor-part-burning"));
            else if (comp.Temperature > _hotTemp)
                args.PushMarkup(Loc.GetString("reactor-part-hot"));
        }
    }

    private static int RadScale(float n)
        => n switch
            {
                > 8f => 5,
                > 6f => 4,
                > 4f => 3,
                _ => (int) Math.Floor(n)
            };

    private void OnIngest(Entity<NuclearPropertiesComponent> ent, ref IngestedEvent args)
    {
        var total = (ent.Comp.NeutronRadioactivity * 20) + (ent.Comp.Radioactivity * 10) + (ent.Comp.SpentFuel * 5);
        var damage = new DamageSpecifier()
        {
            DamageDict = new()
            {
                {Radiation, total}
            }
        };
        _damage.ChangeDamage(args.Target, damage, targetPart: TargetBodyPart.Chest);
    }

    public override void Update(float frameTime)
    {
        // TODO: staggered update
        _accumulator += frameTime;
        if (_accumulator > _threshold)
        {
            AccUpdate();
            _accumulator = 0;
        }
    }

    private void AccUpdate()
    {
        var query = EntityQueryEnumerator<ReactorPartComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (_reactorQuery.HasComp(xform.ParentUid))
                continue; // ignore parts inside of a reactor they're physically separated from the outside

            var gasMix = _atmos.GetTileMixture(uid, true) ?? GasMixture.SpaceGas;
            var DeltaT = (comp.Temperature - gasMix.Temperature) * 0.01f;

            if (Math.Abs(DeltaT) < 0.1)
                continue;

            // This violates the laws of physics, but if energy is conserved, then pulling out a hot rod will turn the room into an oven
            // Also does not take into account thermal mass
            comp.Temperature -= DeltaT;
            if (!gasMix.Immutable) // This prevents it from heating up space itself
                gasMix.Temperature += DeltaT;

            var burncomp = EnsureComp<DamageOnInteractComponent>(uid);

            burncomp.IsDamageActive = comp.Temperature > _hotTemp;

            if (burncomp.IsDamageActive)
            {
                var damage = Math.Max((comp.Temperature - _hotTemp) / _burnDiv, 0);

                burncomp.Damage = new()
                {
                    DamageDict = new()
                    {
                        { Heat, damage }
                    }
                };
            }
            else
            {
                burncomp.Damage = new();
            }

            Dirty(uid, burncomp);
        }
    }
    #endregion

    #region Reactor Methods
    /// <summary>
    /// Processes gas flowing through a reactor part.
    /// </summary>
    /// <param name="ent">The reactor part.</param>
    /// <param name="reactor">The entity representing the reactor this part is inserted into.</param>
    /// <param name="inGas">The gas to be processed.</param>
    /// <returns></returns>
    public GasMixture? ProcessGas(Entity<ReactorPartComponent, NuclearPropertiesComponent, ReactorGasChannelComponent> ent, Entity<NuclearReactorComponent> reactor, GasMixture inGas)
    {
        var (uid, part, props, channel) = ent;
        GasMixture? processedGas = null;
        if (channel.AirContents != null)
        {
            var compTemp = part.Temperature;
            var gasTemp = channel.AirContents.Temperature;

            var deltaT = compTemp - gasTemp;
            var deltaTr = (compTemp + gasTemp) * (compTemp - gasTemp) * (Math.Pow(compTemp, 2) + Math.Pow(gasTemp, 2));

            var k = props.CalculateHeatTransferCoefficient();
            var csa = channel.GasThermalCrossSection * (0.4 * 8);

            var thermalEnergy = _atmos.GetThermalEnergy(channel.AirContents);

            var hottest = Math.Max(gasTemp, compTemp);
            var coldest = Math.Min(gasTemp, compTemp);

            var maxDeltaE = Math.Clamp((k * csa * deltaT) + (5.67037442e-8 * csa * deltaTr),
                (compTemp - hottest) * part.ThermalMass,
                (compTemp - coldest) * part.ThermalMass);

            channel.AirContents.Temperature = (float)Math.Clamp(gasTemp +
                (maxDeltaE / _atmos.GetHeatCapacity(channel.AirContents, true)), coldest, hottest);

            part.Temperature = (float)Math.Clamp(compTemp -
                ((_atmos.GetThermalEnergy(channel.AirContents) - thermalEnergy) / part.ThermalMass), coldest, hottest);

            if (gasTemp < 0 || compTemp < 0)
                throw new Exception("Reactor part temperature went below 0k.");

            if (part.Melted)
            {
                if (_atmos.GetTileMixture(reactor.Owner, excite: true) is { } tile)
                    _atmos.Merge(tile, channel.AirContents);
            }
            else
                processedGas = channel.AirContents;
        }

        if (inGas != null && _atmos.GetThermalEnergy(inGas) > 0)
        {
            channel.AirContents = inGas.RemoveVolume(channel.GasVolume);
            channel.AirContents.Volume = channel.GasVolume;

            if (channel.AirContents.TotalMoles < 1)
            {
                if (processedGas != null)
                    _atmos.Merge(processedGas, channel.AirContents);
                else
                    processedGas = channel.AirContents;
                channel.AirContents.Clear();
            }
        }

        return processedGas;
    }

    /// <summary>
    /// Processes heat transfer within the reactor grid.
    /// </summary>
    /// <param name="part">Reactor part applying the calculations.</param>
    /// <param name="reactor">Reactor housing the reactor part.</param>
    /// <param name="adjacent">List of reactor parts next to the part.</param>
    /// <exception cref="Exception">Calculations resulted in a sub-zero value.</exception>
    public void ProcessHeat(Entity<ReactorPartComponent> part, Entity<NuclearReactorComponent> reactor, List<Entity<ReactorPartComponent>> adjacent)
    {
        // Intercomponent calculation
        var props = _propsQuery.CompOrNull(part);
        double deltaT, k, A;
        foreach (var other in adjacent)
        {
            deltaT = part.Comp.Temperature - other.Comp.Temperature;
            k = NuclearPropertiesComponent.CalculateHeatTransferCoefficient(props, _propsQuery.CompOrNull(other));
            A = Math.Min(part.Comp.ThermalCrossSection, other.Comp.ThermalCrossSection);

            var energy = k * A * (0.5 * 8) * deltaT;
            part.Comp.Temperature = (float)(part.Comp.Temperature - (energy / part.Comp.ThermalMass));
            other.Comp.Temperature = (float)(other.Comp.Temperature + (energy / other.Comp.ThermalMass));

#if DEBUG
            if (part.Comp.Temperature < 0 || other.Comp.Temperature < 0)
                throw new Exception("ReactorPart-ReactorPart temperature calculation resulted in sub-zero value.");
#endif

            ProcessHeatEffects(other);
        }

        // Component-Reactor calculation
        deltaT = part.Comp.Temperature - reactor.Comp.Temperature;

        k = NuclearPropertiesComponent.CalculateHeatTransferCoefficient(props, _propsQuery.CompOrNull(reactor));
        A = part.Comp.ThermalCrossSection;

        part.Comp.Temperature = (float)(part.Comp.Temperature - (k * A * (0.5 * 8) / part.Comp.ThermalMass * deltaT));
        reactor.Comp.Temperature = (float)(reactor.Comp.Temperature - (k * A * (0.5 * 8) / reactor.Comp.ThermalMass * -deltaT));

#if DEBUG
        if (part.Comp.Temperature < 0 || reactor.Comp.Temperature < 0)
            throw new Exception("Reactor-ReactorPart temperature calculation resulted in sub-zero value.");
#endif

        ProcessHeatEffects(part);

        if (part.Comp.Temperature > part.Comp.MeltingPoint && part.Comp.MeltHealth > 0)
            part.Comp.MeltHealth -= _random.Next(10, 50 + 1);
        if (part.Comp.MeltHealth <= 0)
            Melt(part, reactor);

        return;

        void ProcessHeatEffects(Entity<ReactorPartComponent> part)
        {
            var threshold = Atmospherics.T0C + 80; // lol random hardcoded shit
            if (part.Comp.Temperature <= threshold || !_channelQuery.TryComp(part, out var channel) ||
                !_propsQuery.TryComp(part, out var props) || props.ActivePlasma <= 0)
                return;

            var molesPerUnit = 100f; // Arbitrary value for how much gaseous plasma is in each unit of active plasma

            // TODO: clear and reuse
            var payload = new GasMixture();
            payload.SetMoles(Gas.Plasma, (float)Math.Min(props.ActivePlasma * molesPerUnit, Math.Log(((part.Comp.Temperature - threshold) / 100) + 1)));
            payload.Temperature = part.Comp.Temperature;
            props.ActivePlasma -= payload.GetMoles(Gas.Plasma) / molesPerUnit;
            // dont need to dirty since nothing shared/client needs it

            channel.AirContents ??= new GasMixture();
            _atmos.Merge(channel.AirContents, payload);
        }
    }

    /// <summary>
    /// Melts the given reactor part.
    /// </summary>
    /// <param name="part">Reactor part to be melted</param>
    /// <param name="reactor">Reactor housing the reactor part</param>
    public void Melt(Entity<ReactorPartComponent> part, Entity<NuclearReactorComponent> reactor)
    {
        if (part.Comp.Melted)
            return;

        part.Comp.Melted = true;
        DirtyField(part, part.Comp, nameof(ReactorPartComponent.Melted));
        part.Comp.IconStateCap += "_melted_" + _random.Next(1, 4 + 1);
        DirtyField(part, part.Comp, nameof(ReactorPartComponent.IconStateCap));
        part.Comp.NeutronCrossSection = 5f;
        part.Comp.ThermalCrossSection = 20f;
        RemComp<ReactorControlRodComponent>(part);

        if (TryComp<ReactorGasChannelComponent>(part, out var channel))
            channel.GasThermalCrossSection = 0.1f;
    }

    /// <summary>
    /// Returns a list of neutrons from the interation of the given ReactorPart and initial neutrons.
    /// </summary>
    /// <param name="ent">Reactor part applying the calculations.</param>
    /// <param name="neutrons">List of neutrons to be processed.</param>
    /// <param name="thermalEnergy">Thermal energy released from the process.</param>
    /// <returns>Post-processing list of neutrons.</returns>
    public ValueList<ReactorNeutron> ProcessNeutrons(Entity<ReactorPartComponent, NuclearPropertiesComponent> ent, ValueList<ReactorNeutron> neutrons, out float thermalEnergy)
    {
        var (uid, part, props) = ent;
        var preCalcTemp = part.Temperature;
        var flux = new List<ReactorNeutron>(neutrons);
        var isControlRod = _controlQuery.TryComp(uid, out var control);

        // FIXME: holy dogshit performance
        var csa = _rate * part.NeutronCrossSection;
        foreach (var neutron in flux)
        {
            if (_random.Prob(part.ReflectChance)) // reflection
            {
                // A really complicated way of saying do a 180 or a 180+/-45
                neutron.Dir = (neutron.Dir.GetOpposite().ToAngle() + (_random.NextAngle() / 4) - (MathF.Tau / 8)).GetDir();
                continue;
            }

            if (!Prob(props.Density * csa * _bias))
                continue;

            if (neutron.Velocity <= 1 && Prob(_rate * props.NeutronRadioactivity * _bias)) // neutron stimulated emission
            {
                props.NeutronRadioactivity -= _reactant;
                props.Radioactivity += _product;
                for (var i = 0; i < _random.Next(3, 6); i++)
                {
                    neutrons.Add(new(_random.NextAngle().GetDir(), _random.Next(2, 4)));
                }
                neutrons.Remove(neutron);
                part.Temperature += 75f * props.NeutronRadioactivity;
            }
            else if (neutron.Velocity <= 5 && Prob(_rate * props.Radioactivity * _bias)) // stimulated emission
            {
                props.Radioactivity -= _reactant;
                props.SpentFuel += _product;
                for (var i = 0; i < _random.Next(3, 6); i++)
                {
                    neutrons.Add(new(_random.NextAngle().GetDir(), _random.Next(1, 4)));
                }
                neutrons.Remove(neutron);
                part.Temperature += 50f * props.Radioactivity;
            }
            else
            {
                if (isControlRod)
                    neutron.Velocity = 0;
                else
                    neutron.Velocity--;

                if (neutron.Velocity <= 0)
                    neutrons.Remove(neutron);

                part.Temperature += 1; // ... not worth the adjustment
            }
        }
        if (Prob(props.NeutronRadioactivity * csa))
        {
            var count = _random.Next(1, 6);
            for (var i = 0; i < count; i++)
            {
                neutrons.Add(new(_random.NextAngle().GetDir(), 3));
            }
            props.NeutronRadioactivity -= _reactant / 2;
            props.Radioactivity += _product / 2;
        }
        if (Prob(props.Radioactivity * csa))
        {
            var count = _random.Next(1, 6);
            for (var i = 0; i < count; i++)
            {
                neutrons.Add(new(_random.NextAngle().GetDir(), _random.Next(1, 4)));
            }
            props.Radioactivity -= _reactant / 2;
            props.SpentFuel += _product / 2;
        }

        if (isControlRod)
        {
            // cross section of a fuel rod is inversely proportional to its control rod insertion
            var current = 1f - part.NeutronCrossSection;
            var target = control!.ConfiguredInsertionLevel;
            if (!part.Melted && part.NeutronCrossSection != target)
            {
                if (target < current)
                    part.NeutronCrossSection += Math.Min(0.1f, current - target);
                else
                    part.NeutronCrossSection -= Math.Min(0.1f, target - current);
            }
        }

        if (_channelQuery.TryComp(uid, out var channel))
            neutrons = ProcessNeutronsGas((uid, part, channel), neutrons);

        thermalEnergy = (part.Temperature - preCalcTemp) * part.ThermalMass;
        return neutrons;
    }

    /// <summary>
    /// Processes neutrons interacting with gas in a reactor part.
    /// </summary>
    /// <param name="ent">The reactor part to process neutrons for.</param>
    /// <param name="neutrons">The list of neutrons to process.</param>
    /// <returns>The updated list of neutrons after processing.</returns>
    private ValueList<ReactorNeutron> ProcessNeutronsGas(Entity<ReactorPartComponent, ReactorGasChannelComponent> ent, ValueList<ReactorNeutron> neutrons)
    {
        var (_, part, channel) = ent;
        if (channel.AirContents is not { } gas)
            return neutrons;

        var flux = new List<ReactorNeutron>(neutrons);
        foreach (var neutron in flux)
        {
            if (neutron.Velocity <= 0)
                continue;

            var neutronCount = GasNeutronInteract(part, gas);
            if (neutronCount > 1)
            {
                for (var i = 1; i < neutronCount; i++) // starting from 1 since 0 is the current neutron, which isnt being removed
                {
                    neutrons.Add(new(_random.NextAngle().GetDir(), _random.Next(1, 4)));
                }
            }
            else if (neutronCount < 1)
            {
                neutrons.Remove(neutron);
            }
        }

        return neutrons;
    }

    /// <summary>
    /// Performs neutron interactions with the gas in the reactor part.
    /// </summary>
    /// <returns>Change in number of neutrons.</returns>
    private int GasNeutronInteract(ReactorPartComponent part, GasMixture gas)
    {
        // lmao holy hardcoding
        var neutronCount = 1;

        var plasma = gas.GetMoles(Gas.Plasma);
        if (plasma > 1)
        {
            var reactMolPerLiter = 0.25;
            var reactMol = reactMolPerLiter * gas.Volume;

            var plasmaReactCount = (int)Math.Round((plasma - (plasma % reactMol)) / reactMol) + (Prob(plasma - (plasma % reactMol)) ? 1 : 0);
            plasmaReactCount = _random.Next(0, plasmaReactCount + 1);
            gas.AdjustMoles(Gas.Plasma, plasmaReactCount * -0.5f);
            gas.AdjustMoles(Gas.Tritium, plasmaReactCount * 2);
            neutronCount += plasmaReactCount;
        }

        var co2 = gas.GetMoles(Gas.CarbonDioxide);
        if (co2 > 1)
        {
            var reactMolPerLiter = 0.4;
            var reactMol = reactMolPerLiter * gas.Volume;

            var co2ReactCount = (int)Math.Round((co2 - (co2 % reactMol)) / reactMol) + (Prob(co2 - (co2 % reactMol)) ? 1 : 0);
            co2ReactCount = _random.Next(0, co2ReactCount + 1);
            part.Temperature += Math.Min(co2ReactCount, neutronCount);
            neutronCount -= Math.Min(co2ReactCount, neutronCount);
        }

        var tritium = gas.GetMoles(Gas.Tritium);
        if (tritium > 1)
        {
            var reactMolPerLiter = 0.5;
            var reactMol = reactMolPerLiter * gas.Volume;

            var tritiumReactCount = (int)Math.Round((tritium - (tritium % reactMol)) / reactMol) + (Prob(tritium - (tritium % reactMol)) ? 1 : 0);
            tritiumReactCount = _random.Next(0, tritiumReactCount + 1);
            if (tritiumReactCount > 0)
            {
                gas.AdjustMoles(Gas.Tritium, -1 * tritiumReactCount);
                part.Temperature += 1 * tritiumReactCount;
                var (adding, rate) = _random.Next(0, 5) switch
                {
                    0 => (Gas.Oxygen, 0.5f),
                    1 => (Gas.Nitrogen, 0.5f),
                    2 => (Gas.Ammonia, 0.1f),
                    3 => (Gas.NitrousOxide, 0.1f),
                    _ => (Gas.Frezon, 0.1f)
                };
                gas.AdjustMoles(adding, rate * tritiumReactCount);
            }
        }

        return neutronCount;
    }

    /// <summary>
    /// Probablity check that accepts chances > 100%
    /// </summary>
    /// <param name="chance">The chance percentage between 0 and 100.</param>
    private bool Prob(double chance) => _random.NextDouble() <= chance / 100;
    #endregion
}
