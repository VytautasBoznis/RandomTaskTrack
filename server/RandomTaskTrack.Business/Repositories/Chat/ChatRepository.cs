using Dapper;
using RandomTaskTrack.Data.Dtos.Chat;
using RandomTaskTrack.Data.Models.Chat;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Chat;

namespace RandomTaskTrack.Business.Repositories.Chat;

public class ChatRepository : IChatRepository
{
    public async Task<ChatConversation> CreateConversationAsync(ChatConversation conversation, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.chat_conversations (id, title, domain_id)
              VALUES (@Id, @Title, @DomainId)",
            new { conversation.Id, conversation.Title, conversation.DomainId },
            unitOfWork.Transaction);

        return conversation;
    }

    public async Task<ChatConversation?> GetConversationAsync(Guid id, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<ChatConversation>(
            "SELECT id, title, domain_id, created_at, updated_at FROM tracker.chat_conversations WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task<List<ConversationListItemDto>> GetConversationsAsync(int limit, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<ConversationListItemDto>(
            @"SELECT c.id,
                     c.title,
                     c.domain_id,
                     (SELECT count(*) FROM tracker.chat_messages m WHERE m.conversation_id = c.id)::int AS message_count,
                     c.created_at,
                     c.updated_at
              FROM tracker.chat_conversations c
              ORDER BY c.updated_at DESC
              LIMIT @limit",
            new { limit },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(Guid conversationId, IUnitOfWork unitOfWork)
    {
        var rows = await unitOfWork.Connection.QueryAsync<ChatMessage>(
            @"SELECT id, conversation_id, seq, role, content,
                     tool_calls::text   AS tool_calls,
                     tool_results::text AS tool_results,
                     model, input_tokens, output_tokens, created_at
              FROM tracker.chat_messages
              WHERE conversation_id = @conversationId
              ORDER BY seq",
            new { conversationId },
            unitOfWork.Transaction);

        return rows.ToList();
    }

    public async Task<int> GetNextSeqAsync(Guid conversationId, IUnitOfWork unitOfWork)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(max(seq), 0) + 1 FROM tracker.chat_messages WHERE conversation_id = @conversationId",
            new { conversationId },
            unitOfWork.Transaction);
    }

    public async Task AddMessageAsync(ChatMessage message, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            @"INSERT INTO tracker.chat_messages
                  (id, conversation_id, seq, role, content, tool_calls, tool_results, model, input_tokens, output_tokens)
              VALUES
                  (@Id, @ConversationId, @Seq, @Role, @Content, @ToolCalls::jsonb, @ToolResults::jsonb, @Model, @InputTokens, @OutputTokens)",
            new
            {
                message.Id,
                message.ConversationId,
                message.Seq,
                message.Role,
                message.Content,
                message.ToolCalls,
                message.ToolResults,
                message.Model,
                message.InputTokens,
                message.OutputTokens
            },
            unitOfWork.Transaction);
    }

    public async Task TouchConversationAsync(Guid id, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            "UPDATE tracker.chat_conversations SET updated_at = now() WHERE id = @id",
            new { id },
            unitOfWork.Transaction);
    }

    public async Task UpdateTitleAsync(Guid id, string title, IUnitOfWork unitOfWork)
    {
        await unitOfWork.Connection.ExecuteAsync(
            "UPDATE tracker.chat_conversations SET title = @title, updated_at = now() WHERE id = @id",
            new { id, title },
            unitOfWork.Transaction);
    }

    public async Task<bool> DeleteConversationAsync(Guid id, IUnitOfWork unitOfWork)
    {
        int affected = await unitOfWork.Connection.ExecuteAsync(
            "DELETE FROM tracker.chat_conversations WHERE id = @id",
            new { id },
            unitOfWork.Transaction);

        return affected > 0;
    }
}
