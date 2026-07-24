// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BlobCoreComponent : Component
{
    #region Live Data

    [DataField, AutoNetworkedField]
    public EntityUid? Observer = default!;

    [DataField]
    public HashSet<EntityUid> BlobTiles = [];

    [DataField, AutoNetworkedField]
    public List<EntityUid> Actions = [];

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAction;

    [DataField, AutoNetworkedField]
    public ProtoId<BlobChemPrototype> CurrentChem = "ReactiveSpines";

    #endregion

    #region Balance

    [DataField]
    public FixedPoint2 CoreBlobTotalHealth = 400;

    [DataField, AutoNetworkedField]
    public int CurrentPoints = 250; // start with enough for 2 resource nodes and a bit of defensive action

    [DataField]
    public float AttackRate = 0.3f;

    [DataField]
    public float GrowRate = 0.1f;

    [DataField, AutoNetworkedField]
    public bool CanSplit = true;

    #endregion

    #region Blob Costs

    [DataField]
    public int ResourceBlobsTotal;

    [DataField]
    public int AttackCost = 4;

    [DataField]
    public int BlobbernautCost = 60;

    [DataField]
    public int SplitCoreCost = 400;

    [DataField]
    public int SwapCoreCost = 200;

    [DataField]
    public int SwapChemCost = 70;

    #endregion

    #region Blob Ranges

    [DataField]
    public float NodeRadiusLimit = 5f;

    [DataField]
    public float TilesRadiusLimit = 9f;

    #endregion

    #region Prototypes

    [DataField(required: true)]
    public List<EntProtoId> ActionPrototypes = [];

    [DataField]
    public ProtoId<ExplosionPrototype> BlobExplosive = "Blob";

    [DataField]
    public EntProtoId<BlobObserverComponent> ObserverBlobPrototype = "MobObserverBlob";

    [DataField]
    public EntProtoId MindRoleBlobPrototypeId = "MindRoleBlob";

    #endregion

    #region Sounds

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

    [DataField]
    public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/Animals/Blob/blobattack.ogg");

    #endregion
}
