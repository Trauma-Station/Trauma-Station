// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Clears the target entity's access reader lists, then adds some random levels back.
/// </summary>
public sealed partial class ScrambleAccessReader : EntityEffectBase<ScrambleAccessReader>
{
    [DataField]
    public int Min = 1;

    [DataField]
    public int Max = 3;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class ScrambleAccessReaderEffectSystem : EntityEffectSystem<AccessReaderComponent, ScrambleAccessReader>
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    private List<ProtoId<AccessLevelPrototype>> _allLevels = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    protected override void Effect(Entity<AccessReaderComponent> ent, ref EntityEffectEvent<ScrambleAccessReader> args)
    {
        // for airlocks modify the electroncs instead of the door directly
        if (!_access.GetMainAccessReader(ent, out var reader))
            return;

        _access.TryClearAccesses(reader.Value);
        var count = _random.Next(args.Effect.Min, args.Effect.Max);
        for (int i = 0; i < count; i++)
        {
            AddRandomAccess(reader.Value);
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<AccessLevelPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        _allLevels.Clear();
        foreach (var level in _proto.EnumeratePrototypes<AccessLevelPrototype>())
        {
            if (level.CanAddToIdCard)
                _allLevels.Add(level.ID);
        }
    }

    public void AddRandomAccess(Entity<AccessReaderComponent> ent)
    {
        var level = _random.Pick(_allLevels);
        _access.TryAddAccess(ent, level);
    }
}
