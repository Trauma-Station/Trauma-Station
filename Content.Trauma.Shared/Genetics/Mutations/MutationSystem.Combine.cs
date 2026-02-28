// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Mutations;

public sealed partial class MutationSystem
{
    /// <summary>
    /// A dictionary of mutation ids to lists of mutations that require it and could be unlocked by combining.
    /// This is ingredients -> recipes.
    /// </summary>
    public Dictionary<EntProtoId<MutationComponent>, List<ProtoId<MutationRecipePrototype>>> Recipes = new();

    /// <summary>
    /// Every mutation that has a recipe, and its recipes that provide it.
    /// This is result -> recipes.
    /// </summary>
    public Dictionary<EntProtoId<MutationComponent>, List<ProtoId<MutationRecipePrototype>>> ResultRecipes = new();

    private void LoadRecipes()
    {
        Recipes.Clear();
        ResultRecipes.Clear();
        foreach (var recipe in _proto.EnumeratePrototypes<MutationRecipePrototype>())
        {
            var id = recipe.ID;
            // index recipes by the result
            if (ResultRecipes.TryGetValue(recipe.Result, out var recipes))
                recipes.Add(id);
            else
                ResultRecipes[recipe.Result] = new() { id };

            // then by each ingredient
            foreach (var required in recipe.Required)
            {
                if (Recipes.TryGetValue(required, out var results))
                    results.Add(id);
                else
                    Recipes[required] = new() { id };
            }
        }
    }

    #region Public API

    /// <summary>
    /// Get a list of possible mutation combinations that can come from one parent mutation.
    /// </summary>
    public IReadOnlyList<ProtoId<MutationRecipePrototype>> GetPossibleRecipes(EntProtoId<MutationComponent> id)
        => Recipes.TryGetValue(id, out var results)
            ? results
            : [];

    /// <summary>
    /// Returns true if a mutation has at least 1 recipe to combine it.
    /// </summary>
    public bool HasRecipe(EntProtoId<MutationComponent> id)
        => ResultRecipes.ContainsKey(id);

    /// <summary>
    /// Returns a new mutation from two input mutations.
    /// Argument order does not matter.
    /// </summary>
    public EntProtoId<MutationComponent>? CombineMutations(EntProtoId<MutationComponent> a, EntProtoId<MutationComponent> b)
    {
        if (!Recipes.TryGetValue(a, out var results))
            return null;

        foreach (var recipeId in results)
        {
            var recipe = _proto.Index(recipeId);
            // TODO: if you ever want more than 2 required mutations change this function
            if (recipe.Required.Contains(b))
                return recipe.Result;
        }

        return null;
    }

    #endregion
}
