public enum MissionResult
{
    None,
    SerumAcquired,
    OperatorLost
}

public static class MissionResultState
{
    public static MissionResult Result = MissionResult.None;
}
