using AIBase.Abstractions.Chat.Grains;
using AIBase.Abstractions.Chat.Models;
using Microsoft.Extensions.Logging;

namespace AIBase.Core.Grains.Implementations;

public partial class UserGrain
{
    public async Task<List<ChatMember>> GetChatMembershipsAsync()
    {
        if (!state.RecordExists)
            return new List<ChatMember>();

        var chatMembers = new List<ChatMember>();
        var chatIds = state.State.ChatIds;

        foreach (var chatId in chatIds)
        {
            var chatGrain = GrainFactory.GetGrain<IChatGrain>(chatId);
            var chatState = await chatGrain.GetChatStateAsync();
            if (chatState.Status != ChatStatus.Archived)
            {
                chatMembers.Add(new ChatMember(
                    Id: chatId,
                    Type: ChatMemberType.User, // Using User type for chats in user's list
                    Name: chatId, // TODO: Store chat title in user grain
                    JoinedAt: DateTime.UtcNow
                ));
            }
        }

        return chatMembers;
    }

    public async Task AddChatMembershipAsync(ChatMember chatMember)
    {
        if (!state.RecordExists) return;

        if (!state.State.ChatIds.Contains(chatMember.Id))
        {
            state.State.ChatIds.Insert(0, chatMember.Id); // Add to beginning for recent chats
            await state.WriteStateAsync();
            logger.LogInformation("Added chat {ChatId} to user {UserId}", chatMember.Id, state.State.Id);
        }
    }

    public async Task RemoveChatMembershipAsync(string chatId)
    {
        if (!state.RecordExists) return;

        if (state.State.ChatIds.Remove(chatId))
        {
            await state.WriteStateAsync();
            logger.LogInformation("Removed chat {ChatId} from user {UserId}", chatId, state.State.Id);
        }
    }
}