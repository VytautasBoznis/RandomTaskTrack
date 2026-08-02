using RandomTaskTrack.Data.Dtos.Chat;
using RandomTaskTrack.Data.Models.Chat;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Repositories.Chat;

public interface IChatRepository
{
    Task<ChatConversation> CreateConversationAsync(ChatConversation conversation, IUnitOfWork unitOfWork);
    Task<ChatConversation?> GetConversationAsync(Guid id, IUnitOfWork unitOfWork);
    Task<List<ConversationListItemDto>> GetConversationsAsync(int limit, IUnitOfWork unitOfWork);
    Task<List<ChatMessage>> GetMessagesAsync(Guid conversationId, IUnitOfWork unitOfWork);
    Task<int> GetNextSeqAsync(Guid conversationId, IUnitOfWork unitOfWork);
    Task AddMessageAsync(ChatMessage message, IUnitOfWork unitOfWork);
    Task TouchConversationAsync(Guid id, IUnitOfWork unitOfWork);
    Task UpdateTitleAsync(Guid id, string title, IUnitOfWork unitOfWork);
    Task<bool> DeleteConversationAsync(Guid id, IUnitOfWork unitOfWork);
}
