using Content.Shared.Chat.RadioIconsEvents;
using Content.Shared.StatusIcon;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Roles.Jobs;

public abstract partial class SharedJobSystem
{
    public int GetJobGoobcoins(ICommonSession player)
    {
        if (_playerSystem.ContentData(player) is not { Mind: { } mindId }
            || !MindTryGetJob(mindId, out var prototype))
            return 1;

        return prototype.Goobcoins;
    }

    public bool TryFindJobFromIcon(JobIconPrototype jobIcon, [NotNullWhen(true)] out JobPrototype? job)
    {
        foreach (var jobPrototype in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            if (jobPrototype.Icon == jobIcon.ID)
            {
                job = jobPrototype;
                return true;
            }
        }

        job = null;
        return false;
    }
}
