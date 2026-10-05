using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;


namespace Content.Trauma.Shared.Chaplain.ItemBless;

[Prototype]
public sealed partial class BlessablePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<BlessablePrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField]
    public EntProtoId? BlessResult;
}