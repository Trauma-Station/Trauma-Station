using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.Lathe.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Lathe;

/// <summary>
/// Trauma - code for unlock messages, alert level locking, stopping sound.
/// </summary>
public sealed partial class LatheSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private EntityQuery<LatheComponent> _latheQuery;

    private void InitializeTrauma()
    {
        _latheQuery = GetEntityQuery<LatheComponent>();

        SubscribeLocalEvent<LatheComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LatheProducingComponent, ComponentShutdown>(OnProducingShutdown);
    }

    private void OnShutdown(Entity<LatheComponent> ent, ref ComponentShutdown args)
    {
        // destroying a lathe stops its sound
        _audio.Stop(ent.Comp.SoundEntity);
        ent.Comp.SoundEntity = null;
    }

    private void OnProducingShutdown(Entity<LatheProducingComponent> ent, ref ComponentShutdown args)
    {
        // lathe losing power or finishing stops its sound
        if (!_latheQuery.TryComp(ent, out var lathe))
            return;
        lathe.SoundEntity = _audio.Stop(lathe.SoundEntity);
        lathe.SoundEntity = null;
    }

    private void AnnounceAddedRecipes(Entity<LatheComponent> ent, List<ProtoId<LatheRecipePrototype>> recipes)
    {
        if (recipes.Count == 0)
            return;

        var recipesCount = 0;
        foreach (var pack in ent.Comp.DynamicPacks)
        {
            if (!_proto.Resolve(pack, out var proto))
                continue;
            recipesCount += proto.Recipes.Intersect(recipes).Count(); // which recipes we can use are the ones just unlocked?
        }

        if (recipesCount == 0)
            return;

        _chat.TrySendInGameICMessage(ent,
            Loc.GetString("lathe-technology-recipes-update-message", ("count", recipesCount)),
            InGameICChatType.Speak, hideChat: true);
    }

    private string? GetAlertLevel(EntityUid uid)
    {
        var station = _station.GetOwningStation(uid);
        return CompOrNull<AlertLevelComponent>(station)?.CurrentLevel;
    }
}
