using Content.Shared.Store;
using Content.Trauma.Shared.Spy;

namespace Content.Trauma.Client.Spy;

[GenerateTypedNameReferences]
public sealed partial class SpyRewardControl : Control
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IEntityManager _entity = default!;

    public SpyRewardControl(ProtoId<SpyRewardPrototype> id)
    {
    }
}
