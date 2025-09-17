using Chat.ApiModel.Messaging;
using ChatRoom.ApiModel;

namespace Chat.Api.Hubs;

/// <summary>
/// Company messaging related methods pushed by the hub (server-to-client).  
/// </summary>
public interface IMessagingHubPush
{
    /// <summary>
    /// Pushes a new message to the client.
    /// </summary>
    /// <param name="message">The message to push.</param>
    public Task NewMessage(ChatMessageDto message);

    /// <summary>
    /// Pushes an edited message to the client.
    /// </summary>
    /// <param name="message">The edited message to push.</param>
    public Task EditedMessage(ChatMessageDto message);

    /// <summary>
    /// Pushes a deleted message to the client.
    /// </summary>
    /// <param name="id">The ID of the deleted message.</param>
    public Task DeletedMessage(Guid id);

    /// <summary>
    /// Notifie tous les clients qu’une nouvelle chatroom vient d’être créée.
    /// </summary>
    /// <param name="room">La chatroom créée (DTO transmis aux clients).</param>
    Task ChatRoomCreated(Chat.ApiModel.Messaging.ChatRoomDto room);
    
    /// <summary>
    /// Notifie les membres d'un salon qu'un nouvel utilisateur a rejoint.
    /// </summary>
    Task UserJoined(Guid roomId, UserDto newUser);

    /// <summary>
    /// Notifie les membres d'un salon qu'un utilisateur est parti.
    /// </summary>
    Task UserLeft(Guid roomId, Guid userId);

}

/// <summary>
/// Company messaging related methods invoked by the client (client-to-server).
/// </summary>
public interface IMessagingHubInvoke
{
    /// <summary>
    /// Join a chat room to receive new messages, and get the chat history.
    /// </summary>
    /// <param name="roomId">The ID of the chatroom.</param>
    /// <returns>The chat history</returns>
    public Task<IEnumerable<ChatMessageDto>> JoinChatRoom(Guid roomId);

    /// <summary>
    /// Leave the chat room
    /// </summary>
    /// <param name="roomId"></param>
    public Task LeaveChatRoom(Guid roomId);

    /// <summary>
    /// Submits a new message to the chatroom.
    /// </summary>
    public Task SendMessage(string roomId, string message);

    /// <summary>
    /// Get Chat rooom
    /// </summary>
    Task<ChatRoomDto> GetChatRoom(Guid roomId);

    /// <summary>
    /// Create chat room
    /// </summary>
    Task<ChatRoomDto> CreateChatRoom();

    /// <summary>
    /// Create chat room with a specific name.
    /// </summary>
    /// <param name="name">The desired name for the new chatroom.</param>
    Task<ChatRoomDto> CreateChatRoomWithName(string name);

    /// <summary>
    /// Get all chat room
    /// </summary>
    Task<IEnumerable<ChatRoomDto>> GetAllChatRooms();

    /// <summary>
    /// Récupère l'historique des messages pour un salon de discussion spécifique.
    /// </summary>
    /// <param name="roomId">L'ID du salon.</param>
    /// <returns>L'historique des messages du salon.</returns>
    Task<IEnumerable<ChatMessageDto>> GetMessageHistory(Guid roomId);

}