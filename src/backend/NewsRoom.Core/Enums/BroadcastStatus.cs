namespace NewsRoom.Core.Enums;

public enum BroadcastStatus
{
    Pending,
    FetchingNews,
    GeneratingScript,
    GeneratingAudio,
    GeneratingAvatars,
    GeneratingBRoll,
    Composing,
    Completed,
    Failed
}
