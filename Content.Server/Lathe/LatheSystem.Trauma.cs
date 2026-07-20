// SPDX-License-Identifier: AGPL-3.0-or-later

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
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private StationSystem _station = default!;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<LatheComponent> ent, ref ComponentShutdown args)
    {
        // destroying a lathe stops its sound
        _audio.Stop(ent.Comp.SoundEntity);
        ent.Comp.SoundEntity = null;
    }

    private void AnnounceAddedRecipes(Entity<LatheComponent> ent, List<string>? recipes)
    {
        if (recipes is not { } list || list.Count == 0)
            return;

        var recipesCount = 0;
        foreach (var pack in ent.Comp.DynamicPacks)
        {
            if (!ProtoMan.Resolve(pack, out var proto))
                continue;
            foreach (var recipe in proto.Recipes)
            {
                // which recipes we can use are the ones just unlocked?
                if (list.Contains(recipe))
                    recipesCount++;
            }
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
