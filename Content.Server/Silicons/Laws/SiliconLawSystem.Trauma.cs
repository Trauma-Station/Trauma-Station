using Content.Goobstation.Common.Silicons.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Radio;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Research.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Silicons.Laws;

public sealed partial class SiliconLawSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IonLawSystem _ionLaw = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private RadioSystem _radio = default!;

    private void ApplyExperimentalLaws(Entity<SiliconLawUpdaterComponent> ent, Entity<ExperimentalLawProviderComponent, SiliconLawProviderComponent> experiment)
    {
        var laws = GetRandomLaws(experiment.Comp1.RandomLawsets);
        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);

        while (query.MoveNext(out var update))
            SetLaws(laws.Laws, update, experiment.Comp2.LawUploadSound);

        var activeProv = EnsureComp<ActiveExperimentalLawProviderComponent>(ent);
        activeProv.Timer = experiment.Comp1.RewardTime;
        activeProv.RewardPoints = experiment.Comp1.RewardPoints;
        activeProv.OldSiliconLawsetId = ent.Comp.LastLawset;

        var message = Loc.GetString("experimental-law-provider-start", ("timeLeft", (int) experiment.Comp1.RewardTime));
        _radio.SendRadioMessage(ent, message, AnnouncementChannel, ent, escapeMarkup: false);

        QueueDel(experiment); // Don't need this experimental board anymore
    }

    private static readonly ProtoId<RadioChannelPrototype> AnnouncementChannel = "Science";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var activeExperimental = EntityQueryEnumerator<ActiveExperimentalLawProviderComponent>();
        while (activeExperimental.MoveNext(out var uid, out var provider))
        {
            provider.Timer -= frameTime;
            if (provider.Timer >= 0)
                continue;

            // Reward time!!!
            if (!TryComp(uid, out ResearchClientComponent? researchClient) ||
                !researchClient.ConnectedToServer ||
                researchClient.Server == null)
                continue;

            if (!TryComp(uid, out SiliconLawUpdaterComponent? updater))
                continue;

            // Replace laws back
            var lawset = GetLawset(provider.OldSiliconLawsetId).Laws;
            var query = EntityManager.CompRegistryQueryEnumerator(updater.Components);

            while (query.MoveNext(out var update))
                SetLaws(lawset, update, provider.LawRewardSound);

            RemCompDeferred(uid, provider);
            _research.ModifyServerPoints(researchClient.Server.Value, provider.RewardPoints);
            var message = Loc.GetString("experimental-law-provider-success", ("amount", provider.RewardPoints));
            _radio.SendRadioMessage(uid, message, AnnouncementChannel, uid, escapeMarkup: false);
        }
    }

    /// <summary>
    /// Generates random ion storm lawset without an actual silicon.
    /// </summary>
    private SiliconLawset GetRandomLaws(ProtoId<WeightedRandomPrototype> availableSetsId)
    {
        // try to swap it out with a random lawset
        var lawsets = ProtoMan.Index(availableSetsId);
        var lawset = lawsets.Pick(_random);
        var laws = GetLawset(lawset);

        // clone it so not modifying stations lawset
        laws = laws.Clone();

        // shuffle them all
        // hopefully work with existing glitched laws if there are multiple ion storms
        var baseOrder = FixedPoint2.New(1);
        foreach (var law in laws.Laws)
            if (law.Order < baseOrder)
                baseOrder = law.Order;

        _random.Shuffle(laws.Laws);

        // change order based on shuffled position
        for (var i = 0; i < laws.Laws.Count; i++)
            laws.Laws[i].Order = baseOrder + i;

        // remove a random law
        laws.Laws.RemoveAt(_random.Next(laws.Laws.Count));

        // generate a new law...
        var newLaw = _ionLaw.GetIonLaw();

        // see if the law we add will replace a random existing law or be a new glitched order one
        if (laws.Laws.Count > 0)
        {
            var i = _random.Next(laws.Laws.Count);
            laws.Laws[i] = new SiliconLaw()
            {
                LawString = newLaw,
                Order = laws.Laws[i].Order
            };
        }
        else
        {
            laws.Laws.Insert(0,
                new SiliconLaw
                {
                    LawString = newLaw,
                    Order = -1,
                    LawIdentifierOverride = Loc.GetString("ion-storm-law-scrambled-number", ("length", _random.Next(5, 10)))
                });
        }

        // sets all unobfuscated laws' indentifier in order from highest to lowest priority
        // This could technically override the Obfuscation from the code above, but it seems unlikely enough to basically never happen
        var orderDeduction = -1;

        for (var i = 0; i < laws.Laws.Count; i++)
        {
            var notNullIdentifier = laws.Laws[i].LawIdentifierOverride ?? (i - orderDeduction).ToString();

            if (notNullIdentifier.Any(char.IsSymbol))
                orderDeduction += 1;
            else
                laws.Laws[i].LawIdentifierOverride = (i - orderDeduction).ToString();
        }

        return laws;
    }
}
