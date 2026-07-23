using Content.Shared.Humanoid.Markings;

namespace Content.Shared.Body;

public abstract partial class SharedVisualBodySystem
{
    private bool ResolveChildMarkings(Marking marking,
        MarkingPrototype proto,
        List<(Marking, MarkingPrototype)> forcedColors)
    {
        if (marking.IsChildMarking)
            return false;

        foreach (var suffix in proto.ChildMarkingsSuffix)
        {
            if (!ProtoMan.Resolve<MarkingPrototype>($"{marking.MarkingId}{suffix}", out var childProto))
                continue;

            var childMarking = new Marking(childProto.ID, marking.MarkingColors.Count)
            {
                Forced = true,
                IsChildMarking = true,
            };

            forcedColors.Add((childMarking, childProto));
        }

        return true;
    }
}
