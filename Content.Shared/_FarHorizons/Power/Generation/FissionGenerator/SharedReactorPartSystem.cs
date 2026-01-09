using Content.Shared.Atmos;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared._FarHorizons.Materials.Systems;

namespace Content.Shared._FarHorizons.Power.Generation.FissionGenerator;

// Ported and modified from goonstation by Jhrushbe.
// CC-BY-NC-SA-3.0
// https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/reactorcomponents.dm

public abstract class SharedReactorPartSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

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
    /// The amount of a property resultant from a reaction
    /// </summary>
    private readonly float _product = 0.005f;

    #region Methods
    /// <summary>
    /// Melts the related ReactorPart.
    /// </summary>
    /// <param name="reactorPart">Reactor part to be melted</param>
    /// <param name="reactorEnt">Reactor housing the reactor part</param>
    /// <param name="reactorSystem">The SharedNuclearReactorSystem</param>
    public void Melt(ReactorPartComponent reactorPart, Entity<NuclearReactorComponent> reactorEnt, SharedNuclearReactorSystem reactorSystem)
    {
        if (reactorPart.Melted)
            return;

        reactorPart.Melted = true;
        reactorPart.IconStateCap += "_melted_" + _random.Next(1, 4 + 1);
        reactorSystem.UpdateGridVisual(reactorEnt);
        reactorPart.NeutronCrossSection = 5f;
        reactorPart.ThermalCrossSection = 20f;
        reactorPart.IsControlRod = false;

        if(reactorPart.HasRodType(ReactorPartComponent.RodTypes.GasChannel))
            reactorPart.GasThermalCrossSection = 0.1f;
    }

    /// <summary>
    /// Processes heat transfer within the reactor grid.
    /// </summary>
    /// <param name="reactorPart">Reactor part applying the calculations</param>
    /// <param name="reactorEnt">Reactor housing the reactor part</param>
    /// <param name="AdjacentComponents">List of reactor parts next to the reactorPart</param>
    /// <param name="reactorSystem">The SharedNuclearReactorSystem</param>
    /// <exception cref="Exception">Calculations resulted in a sub-zero value</exception>
    public void ProcessHeat(ReactorPartComponent reactorPart, Entity<NuclearReactorComponent> reactorEnt, List<ReactorPartComponent?> AdjacentComponents, SharedNuclearReactorSystem reactorSystem)
    {
        // Intercomponent calculation
        foreach (var RC in AdjacentComponents)
        {
            if (RC == null)
                continue;

            var DeltaT = reactorPart.Temperature - RC.Temperature;
            var k = MaterialSystem.CalculateHeatTransferCoefficient(reactorPart.Properties, RC.Properties);
            var A = Math.Min(reactorPart.ThermalCrossSection, RC.ThermalCrossSection);

            reactorPart.Temperature = (float)(reactorPart.Temperature - (k * A * (0.5 * 8) / reactorPart.ThermalMass * DeltaT));
            RC.Temperature = (float)(RC.Temperature - (k * A * (0.5 * 8) / RC.ThermalMass * -DeltaT));

            if (RC.Temperature < 0 || reactorPart.Temperature < 0)
                throw new Exception("ReactorPart-ReactorPart temperature calculation resulted in sub-zero value.");

            // This is where we'd put material-based temperature effects... IF WE HAD ANY
        }

        // Component-Reactor calculation
        var reactor = reactorEnt.Comp;
        if (reactor != null)
        {
            var DeltaT = reactorPart.Temperature - reactor.Temperature;

            var k = MaterialSystem.CalculateHeatTransferCoefficient(reactorPart.Properties, _proto.Index(reactor.Material).Properties);
            var A = reactorPart.ThermalCrossSection;

            reactorPart.Temperature = (float)(reactorPart.Temperature - (k * A * (0.5 * 8) / reactorPart.ThermalMass * DeltaT));
            reactor.Temperature = (float)(reactor.Temperature - (k * A * (0.5 * 8) / reactor.ThermalMass * -DeltaT));

            if (reactor.Temperature < 0 || reactorPart.Temperature < 0)
                throw new Exception("Reactor-ReactorPart temperature calculation resulted in sub-zero value.");

            // This is where we'd put material-based temperature effects... IF WE HAD ANY
        }
        if (reactorPart.Temperature > reactorPart.MeltingPoint && reactorPart.MeltHealth > 0)
            reactorPart.MeltHealth -= _random.Next(10, 50 + 1);
        if (reactorPart.MeltHealth <= 0)
            Melt(reactorPart, reactorEnt, reactorSystem);
    }

    /// <summary>
    /// Returns a list of neutrons from the interation of the given ReactorPart and initial neutrons.
    /// </summary>
    /// <param name="reactorPart">Reactor part applying the calculations</param>
    /// <param name="neutrons">List of neutrons to be processed</param>
    /// <param name="uid">UID of the host reactor</param>
    /// <param name="thermalEnergy">Thermal energy released from the process</param>
    /// <returns>Post-processing list of neutrons</returns>
    public virtual List<ReactorNeutron> ProcessNeutrons(ReactorPartComponent reactorPart, List<ReactorNeutron> neutrons, EntityUid uid, out float thermalEnergy)
    {
        var preCalcTemp = reactorPart.Temperature;
        var flux = new List<ReactorNeutron>(neutrons);

        foreach (var neutron in flux)
        {
            if (Prob(reactorPart.Properties.Density * _rate * reactorPart.NeutronCrossSection * _bias))
            {
                if (neutron.velocity <= 1 && Prob(_rate * reactorPart.Properties.NeutronRadioactivity * _bias)) // neutron stimulated emission
                {
                    reactorPart.Properties.NeutronRadioactivity -= _reactant;
                    reactorPart.Properties.Radioactivity += _product;
                    for (var i = 0; i < _random.Next(3, 5 + 1); i++) // was 1, 5+1
                    {
                        neutrons.Add(new() { dir = _random.NextAngle().GetDir(), velocity = _random.Next(2, 3 + 1) });
                    }
                    neutrons.Remove(neutron);
                    reactorPart.Temperature += 75f; // Was 50, increased to make neutron reactions stronger
                }
                else if (neutron.velocity <= 5 && Prob(_rate * reactorPart.Properties.Radioactivity * _bias)) // stimulated emission
                {
                    reactorPart.Properties.Radioactivity -= _reactant;
                    reactorPart.Properties.FissileIsotopes += _product;
                    for (var i = 0; i < _random.Next(3, 5 + 1); i++)// was 1, 5+1
                    {
                        neutrons.Add(new() { dir = _random.NextAngle().GetDir(), velocity = _random.Next(1, 3 + 1) });
                    }
                    neutrons.Remove(neutron);
                    reactorPart.Temperature += 50f; // Was 25, increased to make neutron reactions stronger
                }
                else
                {
                    if (Prob(_rate * reactorPart.Properties.Hardness)) // reflection, based on hardness
                        // A really complicated way of saying do a 180 or a 180+/-45
                        neutron.dir = (neutron.dir.GetOpposite().ToAngle() + (_random.NextAngle() / 4) - (MathF.Tau / 8)).GetDir();
                    else if (reactorPart.IsControlRod)
                        neutron.velocity = 0;
                    else
                        neutron.velocity--;

                    if (neutron.velocity <= 0)
                        neutrons.Remove(neutron);

                    reactorPart.Temperature += 1; // ... not worth the adjustment
                }
            }
        }
        if (Prob(reactorPart.Properties.NeutronRadioactivity * _rate * reactorPart.NeutronCrossSection))
        {
            var count = _random.Next(1, 5 + 1); // Was 3+1
            for (var i = 0; i < count; i++)
            {
                neutrons.Add(new() { dir = _random.NextAngle().GetDir(), velocity = 3 });
            }
            reactorPart.Properties.NeutronRadioactivity -= _reactant / 2;
            reactorPart.Properties.Radioactivity += _product / 2;
            //This code has been deactivated so neutrons would have a bigger impact
            //reactorPart.Temperature += 13; // 20 * 0.65
        }
        if (Prob(reactorPart.Properties.Radioactivity * _rate * reactorPart.NeutronCrossSection))
        {
            var count = _random.Next(1, 5 + 1); // Was 3+1
            for (var i = 0; i < count; i++)
            {
                neutrons.Add(new() { dir = _random.NextAngle().GetDir(), velocity = _random.Next(1, 3 + 1) });
            }
            reactorPart.Properties.Radioactivity -= _reactant / 2;
            reactorPart.Properties.FissileIsotopes += _product / 2;
            //This code has been deactivated so neutrons would have a bigger impact
            //reactorPart.Temperature += 6.5f; // 10 * 0.65
        }

        if (reactorPart.HasRodType(ReactorPartComponent.RodTypes.ControlRod))
        {
            if (!reactorPart.Melted && (reactorPart.NeutronCrossSection != reactorPart.ConfiguredInsertionLevel))
            {
                if (reactorPart.ConfiguredInsertionLevel < reactorPart.NeutronCrossSection)
                    reactorPart.NeutronCrossSection -= Math.Min(0.1f, reactorPart.NeutronCrossSection - reactorPart.ConfiguredInsertionLevel);
                else
                    reactorPart.NeutronCrossSection += Math.Min(0.1f, reactorPart.ConfiguredInsertionLevel - reactorPart.NeutronCrossSection);
            }
        }

        if (reactorPart.HasRodType(ReactorPartComponent.RodTypes.GasChannel))
            neutrons = ProcessNeutronsGas(reactorPart, neutrons);

        neutrons ??= [];
        thermalEnergy = (reactorPart.Temperature - preCalcTemp) * reactorPart.ThermalMass;
        return neutrons;
    }

    /// <summary>
    /// Returns a list of neutrons from the interation of the gasses within the given ReactorPart and initial neutrons.
    /// </summary>
    /// <param name="reactorPart">Reactor part applying the calculations</param>
    /// <param name="neutrons">List of neutrons to be processed</param>
    /// <returns>Post-processing list of neutrons</returns>
    public virtual List<ReactorNeutron> ProcessNeutronsGas(ReactorPartComponent reactorPart, List<ReactorNeutron> neutrons) => neutrons;

    /// <summary>
    /// Returns true according to a percent chance.
    /// </summary>
    /// <param name="chance">Double, 0-100 </param>
    /// <returns></returns>
    protected bool Prob(double chance) => _random.NextDouble() <= chance / 100;

    #endregion
}