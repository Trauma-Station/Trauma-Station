// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.CollectiveMind;

public sealed class CollectiveMindUpdateSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static Dictionary<string, int> _currentId = new();

    public void UpdateCollectiveMind(EntityUid uid, CollectiveMindComponent collective)
    {
        foreach (var prototype in _proto.EnumeratePrototypes<CollectiveMindPrototype>())
        {
            if (!_currentId.ContainsKey(prototype.ID))
                _currentId[prototype.ID] = 0;

            bool hasChannel = collective.Channels.Contains(prototype.ID);
            bool alreadyAdded = collective.Minds.ContainsKey(prototype.ID);
            if (hasChannel && !alreadyAdded)
                collective.Minds.Add(prototype.ID, ++_currentId[prototype.ID]);
            else if (!hasChannel && alreadyAdded)
                collective.Minds.Remove(prototype.ID);
        }
    }
}
