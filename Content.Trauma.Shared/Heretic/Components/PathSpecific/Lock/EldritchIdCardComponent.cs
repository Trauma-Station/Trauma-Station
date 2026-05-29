// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Access;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class EldritchIdCardComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EldritchIdConfiguration> Configs = new();

    [DataField, AutoNetworkedField]
    public EntProtoId? CurrentProto;

    [DataField, AutoNetworkedField]
    public bool Inverted;

    [DataField, AutoNetworkedField]
    public EntityUid? PortalOne;

    [DataField, AutoNetworkedField]
    public EntityUid? PortalTwo;

    [DataField]
    public EntProtoId Portal = "RealityCrack";

    [DataField]
    public SoundSpecifier EatSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/eatfood.ogg");
}

[Serializable, NetSerializable]
public sealed class EldritchIdConfiguration(
    string? fullName,
    string? jobTitle,
    ProtoId<JobIconPrototype> jobIcon,
    List<ProtoId<DepartmentPrototype>> departments,
    EntProtoId cardPrototype)
{
    public readonly string? FullName = fullName;
    public readonly string? JobTitle = jobTitle;
    public readonly ProtoId<JobIconPrototype> JobIcon = jobIcon;
    public readonly List<ProtoId<DepartmentPrototype>> Departments = departments;
    public readonly EntProtoId CardPrototype = cardPrototype;

    public override bool Equals(object? obj)
    {
        if (obj is not EldritchIdConfiguration other)
            return false;

        return FullName == other.FullName &&
               JobTitle == other.JobTitle &&
               JobIcon.Id == other.JobIcon.Id &&
               CardPrototype.Id == other.CardPrototype.Id &&
               Departments.SequenceEqual(other.Departments);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(FullName);
        hash.Add(JobTitle);
        hash.Add(JobIcon.Id);
        hash.Add(CardPrototype.Id);

        hash.Add(Departments.Count);
        foreach (var dept in Departments)
        {
            hash.Add(dept);
        }

        return hash.ToHashCode();
    }
}
