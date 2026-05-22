// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EldritchInfluenceComponent : Component
{
    [DataField]
    public bool Spent;

    [DataField]
    public SoundSpecifier? ExamineSound = new SoundCollectionSpecifier("bloodCrawl");

    [DataField]
    public LocId ExamineBaseMessage = "influence-base-message";

    [DataField]
    public int FontSize = 22;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> HeathenExamineMessages = "FractureHeathenExamineMessages";

    [DataField]
    public List<EntityEffect[]> PossibleExamineEffects = new();

    [DataField]
    public EntProtoId ExaminedRiftStatusEffect = "ExaminedRiftStatusEffect";

    [DataField]
    public TimeSpan ExamineDelay = TimeSpan.FromSeconds(1);
}
