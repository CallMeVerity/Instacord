namespace Instacord.Bot.Preconditions;

internal static class OwnerGate
{
    public static bool IsOwner(ulong userId, ulong ownerId) => userId == ownerId;
}