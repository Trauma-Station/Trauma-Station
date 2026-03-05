using System.Linq;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction;

/// <summary>
/// Trauma - helper for shared code to call server methods
/// </summary>
public sealed partial class ConstructionSystem
{
    public override bool ChangeNode(EntityUid uid, EntityUid? userUid, string id, bool performActions = true)
        => ChangeNode(uid, userUid, id, performActions, null);

    /// <summary>
    /// Passes constructionPrototype entiry into the modifier component
    /// </summary>
    /// <param name="item"></param>
    /// <param name="constructionPrototype"></param>
    public void EnsureConstructionKnowledge(EntityUid item, ConstructionPrototype constructionPrototype, EntityUid user)
    {
        EnsureComp<QualityComponent>(item, out var quality);
        foreach (var construct in constructionPrototype.Groups)
        {
            quality.LevelDeltas[construct.Key] = construct.Value;
        }
        quality.QualityCoefficent = constructionPrototype.QualityCoefficient;
        if (!HasComp<KnowledgeHolderComponent>(user))
            return;
        var ev = new UpdateItemQualityEvent(user);
        RaiseLocalEvent(item, ref ev);
    }

    public void TransferQuality(EntityUid original, EntityUid created)
    {
        if (!TryComp<QualityComponent>(original, out var originalComp))
            return;

        if (TryComp<QualityComponent>(created, out var newComp))
        {
            var quality = newComp.Quality * originalComp.NumberOfMasteries;
            quality += originalComp.Quality;
            newComp.NumberOfMasteries = originalComp.NumberOfMasteries + 1;
            newComp.Quality = quality / newComp.NumberOfMasteries;
            newComp.QualityCoefficent = (newComp.QualityCoefficent + originalComp.QualityCoefficent) / 2;
            Dirty(created, newComp);
            return;
        }
        newComp = EnsureComp<QualityComponent>(created);
        newComp.LevelDeltas = originalComp.LevelDeltas;
        newComp.Quality = originalComp.Quality;
        newComp.NumberOfMasteries = originalComp.NumberOfMasteries;
        newComp.QualityCoefficent = originalComp.QualityCoefficent;
        Dirty(created, newComp);
        return;
    }
}
