using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Reaction;

public sealed partial class ChemicalReactionSystem
{

    /// <summary>
    ///     Continually react a RAW solution object (like one in a PipeNet) until stable.
    /// </summary>
    public void FullyReactRaw(Solution solution, EntityUid? location = null, ReactionMixerComponent? mixer = null)
    {
        SortedSet<ReactionPrototype> reactions = new();
        foreach (var reactant in solution.Contents)
        {
            if (_reactionsSingle.TryGetValue(reactant.Reagent.Prototype, out var reactantReactions))
                reactions.UnionWith(reactantReactions);
        }

        for (var i = 0; i < MaxReactionIterations; i++)
        {
            if (!ProcessReactionsRaw(solution, reactions, mixer, location))
                return;
        }

        // Log error if it loops infinitely
        Log.Error($"Solution in pipe at {location} exceeded reaction limit!");
    }

    /// <summary>
    ///     Performs all chemical reactions that can be run on a solution.
    ///     Removes the reactants from the solution, then returns a solution with all products.
    ///     WARNING: Does not trigger reactions between solution and new products.
    /// </summary>
    private bool ProcessReactionsRaw(Solution solution, SortedSet<ReactionPrototype> reactions, ReactionMixerComponent? mixerComponent, EntityUid? location = null)
    {
        List<string>? products = null;

        // attempt to perform any applicable reaction
        foreach (var reaction in reactions)
        {
            if (!CanReactRaw(solution, reaction, mixerComponent, out var unitReactions))
            {
                continue;
            }

            products = PerformReactionRaw(solution, reaction, unitReactions, location);
            break;
        }

        // did any reaction occur?
        if (products == null)
            return false;

        if (products.Count == 0)
            return true;

        // Add any reactions associated with the new products. This may re-add reactions that were already iterated
        // over previously. The new product may mean the reactions are applicable again and need to be processed.
        foreach (var product in products)
        {
            if (_reactions.TryGetValue(product, out var reactantReactions))
                reactions.UnionWith(reactantReactions);
        }

        return true;
    }

    /// <summary>
    ///     Perform a reaction on a solution. This assumes all reaction criteria are met.
    ///     Removes the reactants from the solution, adds products, and returns a list of products.
    /// </summary>
    private List<string> PerformReactionRaw(Solution solution, ReactionPrototype reaction, FixedPoint2 unitReactions, EntityUid? location = null)
    {
        var energy = reaction.ConserveEnergy ? solution.GetThermalEnergy(_prototypeManager) : 0;

        //Remove reactants
        foreach (var reactant in reaction.Reactants)
        {
            if (!reactant.Value.Catalyst)
            {
                var amountToRemove = unitReactions * reactant.Value.Amount;
                solution.RemoveReagent(reactant.Key, amountToRemove, ignoreReagentData: true);
            }
        }

        //Create products
        var products = new List<string>();
        foreach (var product in reaction.Products)
        {
            products.Add(product.Key);
            solution.AddReagent(product.Key, product.Value * unitReactions);
        }

        if (reaction.ConserveEnergy)
        {
            var newCap = solution.GetHeatCapacity(_prototypeManager);
            if (newCap > 0)
                solution.Temperature = energy / newCap;
        }

        OnReactionRaw(solution, reaction, location, unitReactions);

        return products;
    }

    /// <summary>
    ///     Checks if a solution can undergo a specified reaction.
    /// </summary>
    /// <param name="solution">The solution to check.</param>
    /// <param name="reaction">The reaction to check.</param>
    /// <param name="lowestUnitReactions">How many times this reaction can occur.</param>
    /// <returns></returns>
    private bool CanReactRaw(Solution solution, ReactionPrototype reaction, ReactionMixerComponent? mixerComponent, out FixedPoint2 lowestUnitReactions, EntityUid? location = null)
    {
        lowestUnitReactions = FixedPoint2.MaxValue;
        if (solution.Temperature < reaction.MinimumTemperature || solution.Temperature > reaction.MaximumTemperature)
        {
            lowestUnitReactions = FixedPoint2.Zero;
            return false;
        }

        if ((mixerComponent is not { } && reaction.MixingCategories is { }) ||
            mixerComponent is { } && reaction.MixingCategories is { } && reaction.MixingCategories.Except(mixerComponent.ReactionTypes).Any())
        {
            lowestUnitReactions = FixedPoint2.Zero;
            return false;
        }

        /* Disabled for now, needs to shit upstream.
        if (location is { } locationTrue)
        {
            var attempt = new ReactionAttemptEvent(reaction, locationTrue);
            RaiseLocalEvent(locationTrue, ref attempt);
            if (attempt.Cancelled)
            {
                lowestUnitReactions = FixedPoint2.Zero;
                return false;
            }

        }
        */

        foreach (var reactantData in reaction.Reactants)
        {
            var reactantName = reactantData.Key;
            var reactantCoefficient = reactantData.Value.Amount;

            var reactantQuantity = solution.GetTotalPrototypeQuantity(reactantName);

            if (reactantQuantity <= FixedPoint2.Zero)
                return false;

            if (reactantData.Value.Catalyst)
            {
                // catalyst is not consumed, so will not limit the reaction. But it still needs to be present, and
                // for quantized reactions we need to have a minimum amount

                if (reactantQuantity == FixedPoint2.Zero || reaction.Quantized && reactantQuantity < reactantCoefficient)
                    return false;

                continue;
            }

            var unitReactions = reactantQuantity / reactantCoefficient;

            if (unitReactions < lowestUnitReactions)
                lowestUnitReactions = unitReactions;
        }

        if (reaction.Quantized)
            lowestUnitReactions = (int) lowestUnitReactions;

        return lowestUnitReactions > 0;
    }

    private void OnReactionRaw(Solution solution, ReactionPrototype reaction, EntityUid? location, FixedPoint2 unitReactions)
    {
        var coordsString = "[No Location]";
        if (location is { })
        {
            var posFound = _transformSystem.TryGetMapOrGridCoordinates(location.Value, out var gridPos);
            coordsString = posFound ? $"{gridPos}" : "[Grid/Map not Found]";
        }

        _adminLogger.Add(LogType.ChemicalReaction, reaction.Impact,
            $"Plumbing reaction {reaction.ID:reaction} occurred with strength {unitReactions:strength} at {coordsString}");

        if (location is { })
            _entityEffects.ApplyEffects(location.Value, reaction.Effects, unitReactions);

        // Someday, some brave soul will thread through an optional actor
        // argument in from every call of OnReaction up, all just to pass
        // it to PlayPredicted. I am not that brave soul.
        if (_netMan.IsServer && reaction.Sound != null && location is { } locationTrue)
            _audio.PlayPvs(reaction.Sound, locationTrue);
    }
}
