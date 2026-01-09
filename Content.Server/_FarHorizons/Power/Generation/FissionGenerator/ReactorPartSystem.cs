using Content.Server.Atmos.EntitySystems;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Content.Shared.Atmos;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Robust.Shared.Random;
using Content.Shared._FarHorizons.Materials.Systems;
using Content.Shared.Examine;
using Content.Shared.Nutrition;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage;
using Content.Shared.Radiation.Components;
using Content.Shared.Damage.Components;
using Content.Server.Atmos.Piping.Components;

namespace Content.Server._FarHorizons.Power.Generation.FissionGenerator;

// Ported and modified from goonstation by Jhrushbe.
// CC-BY-NC-SA-3.0
// https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/reactorcomponents.dm

public sealed class ReactorPartSystem : SharedReactorPartSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;

    /// <summary>
    /// Temperature (in C) when people's hands can be burnt
    /// </summary>
    private readonly static float _hotTemp = 80;

    /// <summary>
    /// Temperature (in C) when insulated gloves can no longer protect
    /// </summary>
    private readonly static float _burnTemp = 400;

    private readonly static float _burnDiv = (_burnTemp - _hotTemp) / 5; // The 5 is how much heat damage insulated gloves protect from

    private readonly float _threshold = 1f;
    private float _accumulator = 0f;

    #region Item Methods
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReactorPartComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ReactorPartComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ReactorPartComponent, IngestedEvent>(OnIngest);
    }

    private void OnInit(EntityUid uid, ReactorPartComponent component, ref MapInitEvent args)
    {
        var radvalue = (component.Properties.Radioactivity * 0.1f) + (component.Properties.NeutronRadioactivity * 0.15f) + (component.Properties.FissileIsotopes * 0.125f);
        if (radvalue > 0)
        {
            var radcomp = EnsureComp<RadiationSourceComponent>(uid);
            radcomp.Intensity = radvalue;
        }

        if (component.Properties.NeutronRadioactivity > 0)
        {
            var lightcomp = _lightSystem.EnsureLight(uid);
            _lightSystem.SetEnergy(uid, component.Properties.NeutronRadioactivity, lightcomp);
            _lightSystem.SetColor(uid, Color.FromHex("#22bbff"), lightcomp);
            _lightSystem.SetRadius(uid, 1.2f, lightcomp);
        }
    }

    private void OnExamine(Entity<ReactorPartComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(ReactorPartComponent)))
        {
            switch (comp.Properties.NeutronRadioactivity)
            {
                case > 8:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-5"));
                    break;
                case > 6:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-4"));
                    break;
                case > 4:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-3"));
                    break;
                case > 2:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-2"));
                    break;
                case > 1:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-1"));
                    break;
                case > 0:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-0"));
                    break;
            }

            switch (comp.Properties.Radioactivity)
            {
                case > 8:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-5"));
                    break;
                case > 6:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-4"));
                    break;
                case > 4:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-3"));
                    break;
                case > 2:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-2"));
                    break;
                case > 1:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-1"));
                    break;
                case > 0:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-0"));
                    break;
            }

            if (comp.Temperature > Atmospherics.T0C + _burnTemp)
                args.PushMarkup(Loc.GetString("reactor-part-burning"));
            else if (comp.Temperature > Atmospherics.T0C + _hotTemp)
                args.PushMarkup(Loc.GetString("reactor-part-hot"));
        }
    }

    private void OnIngest(Entity<ReactorPartComponent> ent, ref IngestedEvent args)
    {
        var comp = ent.Comp;
        if (comp.Properties == null)
            return;

        var properties = comp.Properties;

        if (!_entityManager.TryGetComponent<DamageableComponent>(args.Target, out var damageable) || damageable.Damage.DamageDict == null)
            return;

        var dict = damageable.Damage.DamageDict;

        var dmgKey = "Radiation";
        var dmg = properties.NeutronRadioactivity * 20 + properties.Radioactivity * 10 + properties.FissileIsotopes * 5;

        if (!dict.TryAdd(dmgKey, dmg))
        {
            var prev = dict[dmgKey];
            dict.Remove(dmgKey);
            dict.Add(dmgKey, prev + dmg);
        }
    }

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator > _threshold)
        {
            AccUpdate();
            _accumulator = 0;
        }
    }

    private void AccUpdate()
    {
        var query = EntityQueryEnumerator<ReactorPartComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var gasMix = _atmosphereSystem.GetTileMixture(uid, true) ?? GasMixture.SpaceGas;
            var DeltaT = (component.Temperature - gasMix.Temperature) * 0.01f;

            if (Math.Abs(DeltaT) < 0.1)
                continue;

            // This viloates the laws of physics, but if energy is conserved, then pulling out a hot rod will turn the room into an oven
            // Also does not take into account thermal mass
            component.Temperature -= DeltaT;
            if (!gasMix.Immutable) // This prevents it from heating up space itself
                gasMix.Temperature += DeltaT;

            var burncomp = EnsureComp<DamageOnInteractComponent>(uid);

            burncomp.IsDamageActive = component.Temperature > Atmospherics.T0C + _hotTemp;

            if (burncomp.IsDamageActive)
            {
                var damage = Math.Max((component.Temperature - Atmospherics.T0C - _hotTemp) / _burnDiv, 0);

                // Giant string of if/else that makes sure it will interfere only as much as it needs to
                if (burncomp.Damage == null)
                    burncomp.Damage = new() { DamageDict = new() { { "Heat", damage } } };
                else if (burncomp.Damage.DamageDict == null)
                    burncomp.Damage.DamageDict = new() { { "Heat", damage } };
                else if (!burncomp.Damage.DamageDict.ContainsKey("Heat"))
                    burncomp.Damage.DamageDict.Add("Heat", damage);
                else
                    burncomp.Damage.DamageDict["Heat"] = damage;
            }

            Dirty(uid, burncomp);
        }
    }
    #endregion

    /// <summary>
    /// Processes gas flowing through a reactor part.
    /// </summary>
    /// <param name="reactorPart">The reactor part.</param>
    /// <param name="reactorEnt">The entity representing the reactor this part is inserted into.</param>
    /// <param name="inGas">The gas to be processed.</param>
    /// <returns></returns>
    public GasMixture? ProcessGas(ReactorPartComponent reactorPart, Entity<NuclearReactorComponent> reactorEnt, GasMixture inGas)
    {
        if (!reactorPart.HasRodType(ReactorPartComponent.RodTypes.GasChannel))
            return null;

        GasMixture? ProcessedGas = null;
        if (reactorPart.AirContents != null)
        {
            var compTemp = reactorPart.Temperature;
            var gasTemp = reactorPart.AirContents.Temperature;

            var DeltaT = compTemp - gasTemp;
            var DeltaTr = (compTemp + gasTemp) * (compTemp - gasTemp) * (Math.Pow(compTemp, 2) + Math.Pow(gasTemp, 2));

            var k = MaterialSystem.CalculateHeatTransferCoefficient(reactorPart.Properties, null);
            var A = reactorPart.GasThermalCrossSection * (0.4 * 8);

            var ThermalEnergy = _atmosphereSystem.GetThermalEnergy(reactorPart.AirContents);

            var Hottest = Math.Max(gasTemp, compTemp);
            var Coldest = Math.Min(gasTemp, compTemp);

            var MaxDeltaE = Math.Clamp((k * A * DeltaT) + (5.67037442e-8 * A * DeltaTr),
                (compTemp * reactorPart.ThermalMass) - (Hottest * reactorPart.ThermalMass),
                (compTemp * reactorPart.ThermalMass) - (Coldest * reactorPart.ThermalMass));

            reactorPart.AirContents.Temperature = (float)Math.Clamp(gasTemp +
                (MaxDeltaE / _atmosphereSystem.GetHeatCapacity(reactorPart.AirContents, true)), Coldest, Hottest);

            reactorPart.Temperature = (float)Math.Clamp(compTemp -
                ((_atmosphereSystem.GetThermalEnergy(reactorPart.AirContents) - ThermalEnergy) / reactorPart.ThermalMass), Coldest, Hottest);

            if (gasTemp < 0 || compTemp < 0)
                throw new Exception("Reactor part temperature went below 0k.");

            if (reactorPart.Melted)
            {
                var T = _atmosphereSystem.GetTileMixture(reactorEnt.Owner, excite: true);
                if (T != null)
                    _atmosphereSystem.Merge(T, reactorPart.AirContents);
            }
            else
                ProcessedGas = reactorPart.AirContents;
        }

        if (inGas != null && _atmosphereSystem.GetThermalEnergy(inGas) > 0)
        {
            reactorPart.AirContents = inGas.RemoveVolume(reactorPart.GasVolume);
            reactorPart.AirContents.Volume = reactorPart.GasVolume;

            if (reactorPart.AirContents != null && reactorPart.AirContents.TotalMoles < 1)
            {
                if (ProcessedGas != null)
                {
                    _atmosphereSystem.Merge(ProcessedGas, reactorPart.AirContents);
                    reactorPart.AirContents.Clear();
                }
                else
                {
                    ProcessedGas = reactorPart.AirContents;
                    reactorPart.AirContents.Clear();
                }
            }
        }
        return ProcessedGas;
    }

    /// <inheritdoc/>
    public override List<ReactorNeutron> ProcessNeutronsGas(ReactorPartComponent reactorPart, List<ReactorNeutron> neutrons)
    {
        if (reactorPart.AirContents == null) return neutrons;

        var flux = new List<ReactorNeutron>(neutrons);
        foreach (var neutron in flux)
        {
            if (neutron.velocity > 0)
            {
                var neutronCount = GasNeutronInteract(reactorPart);
                if (neutronCount > 1)
                    for (var i = 0; i < neutronCount; i++)
                        neutrons.Add(new() { dir = _random.NextAngle().GetDir(), velocity = _random.Next(1, 3 + 1) });
                else if (neutronCount < 1)
                    neutrons.Remove(neutron);
            }
        }

        return neutrons;
    }

    /// <summary>
    /// Determines the number of additional neutrons the gas makes.
    /// </summary>
    /// <param name="reactorPart"></param>
    /// <returns>Change in number of neutrons</returns>
    private int GasNeutronInteract(ReactorPartComponent reactorPart)
    {
        if (reactorPart.AirContents == null)
            return 1;

        var neutronCount = 1;
        var gas = reactorPart.AirContents;

        if (gas.GetMoles(Gas.Plasma) > 1)
        {
            var reactMolPerLiter = 0.25;
            var reactMol = reactMolPerLiter * gas.Volume;

            var plasma = gas.GetMoles(Gas.Plasma);
            var plasmaReactCount = (int)Math.Round((plasma - (plasma % reactMol)) / reactMol) + (Prob(plasma - (plasma % reactMol)) ? 1 : 0);
            plasmaReactCount = _random.Next(0, plasmaReactCount + 1);
            gas.AdjustMoles(Gas.Plasma, plasmaReactCount * -0.5f);
            gas.AdjustMoles(Gas.Tritium, plasmaReactCount * 2);
            neutronCount += plasmaReactCount;
        }

        if (gas.GetMoles(Gas.CarbonDioxide) > 1)
        {
            var reactMolPerLiter = 0.4;
            var reactMol = reactMolPerLiter * gas.Volume;

            var co2 = gas.GetMoles(Gas.CarbonDioxide);
            var co2ReactCount = (int)Math.Round((co2 - (co2 % reactMol)) / reactMol) + (Prob(co2 - (co2 % reactMol)) ? 1 : 0);
            co2ReactCount = _random.Next(0, co2ReactCount + 1);
            reactorPart.Temperature += Math.Min(co2ReactCount, neutronCount);
            neutronCount -= Math.Min(co2ReactCount, neutronCount);
        }

        if (gas.GetMoles(Gas.Tritium) > 1)
        {
            var reactMolPerLiter = 0.5;
            var reactMol = reactMolPerLiter * gas.Volume;

            var tritium = gas.GetMoles(Gas.Tritium);
            var tritiumReactCount = (int)Math.Round((tritium - (tritium % reactMol)) / reactMol) + (Prob(tritium - (tritium % reactMol)) ? 1 : 0);
            tritiumReactCount = _random.Next(0, tritiumReactCount + 1);
            if (tritiumReactCount > 0)
            {
                gas.AdjustMoles(Gas.Tritium, -1 * tritiumReactCount);
                reactorPart.Temperature += 1 * tritiumReactCount;
                switch (_random.Next(0, 5))
                {
                    case 0:
                        gas.AdjustMoles(Gas.Oxygen, 0.5f * tritiumReactCount);
                        break;
                    case 1:
                        gas.AdjustMoles(Gas.Nitrogen, 0.5f * tritiumReactCount);
                        break;
                    case 2:
                        gas.AdjustMoles(Gas.Ammonia, 0.1f * tritiumReactCount);
                        break;
                    case 3:
                        gas.AdjustMoles(Gas.NitrousOxide, 0.1f * tritiumReactCount);
                        break;
                    case 4:
                        gas.AdjustMoles(Gas.Frezon, 0.1f * tritiumReactCount);
                        break;
                    default:
                        break;
                }
            }
        }

        return neutronCount;
    }
}