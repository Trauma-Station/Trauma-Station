namespace  Content.Trauma.Common.MartialArts;

public enum GrabStage
{
    No = 0,
    Soft = 1,
    Hard = 2,
    Suffocate = 3,
}

public enum GrabStageDirection
{
    Increase,
    Decrease,
}

public enum GrabResistResult
{
    TooSoon,
    Failed,
    Succeeded
}
