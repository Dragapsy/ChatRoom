using System.Text.Json;
using System.Text.Json.Serialization;
using Chat.Business.Persistance;
using Chat.Model;
using Chat.Model.Messaging;
using Microsoft.Extensions.Logging;

namespace Chat.Business.Messaging;

/// <summary>
/// Provides functionality for sending messages between companies.
/// </summary>
public sealed class MessagingService
{
    private readonly IMessagingPersistance _messagingPersistance;
    private readonly IMessagingNotificationHandler _notificationHandler;
    private readonly IUserPersistance _userPersistance;
    private readonly ILogger<MessagingService> Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingService"/> class.
    /// </summary>
    public MessagingService(IMessagingPersistance messagingPersistance,
        IMessagingNotificationHandler notificationHandler,
        IUserPersistance userPersistance,
        ILogger<MessagingService> logger)
    {
        _messagingPersistance = messagingPersistance;
        _notificationHandler = notificationHandler;
        _userPersistance = userPersistance;
        Logger = logger;
    }

    /// <summary>
    /// Gets all messages.
    /// </summary>
    /// <returns>All messages.</returns>
    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(Guid roomId, CancellationToken ct) => 
        await _messagingPersistance.GetMessages(roomId, ct);

    /// <summary>
    /// Gets all messages in a chat room.
    /// </summary>
    /// <param name="roomId">ID of the chat room.</param>
    /// <returns>All messages in the chat room.</returns>
    public IEnumerable<ChatMessage> GetMessagesInRoom(Guid roomId) =>
        _messagingPersistance.GetMessagesInRoom(roomId);

    /// <summary>
    /// Submits a new chat message.
    /// </summary>
    public async Task SubmitMessageAsync(string roomId, string message, string nameIdentifier, CancellationToken ct = default)
    {
        Model.Messaging.ChatRoom chatRoom = await _messagingPersistance.GetChatRoomAsync(Guid.Parse(roomId), ct) 
                                            ?? throw new ArgumentException($"Room {roomId} not found");
        
        User user = await GetUserFromNameIdentifier(nameIdentifier);
        
        ChatMessage chatMessage = new ChatMessage()
        {
            Content = message,
            CreatedAt = DateTime.UtcNow,
            AuthorId = user.Id,
            Author = user,
            Room = chatRoom,
            RoomId = chatRoom.Id
        };
        
        Logger.LogDebug("Received a chat message: {chatMessage}", JsonSerializer.Serialize(chatMessage, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve }));
        
        await _messagingPersistance.SubmitMessageAsync(chatMessage, ct);
        
        await _notificationHandler.NotifyNewMessageAsync(chatMessage);
    }

    /// <summary>
    ///  Get user from name identifier.
    /// </summary>
    private async Task<User> GetUserFromNameIdentifier(string nameIdentifier)
    {
        User user = await _userPersistance.GetUserByUsernameAsync(nameIdentifier) 
                    ?? throw new ArgumentException($"User {nameIdentifier} not found");
        return user;
    }
        
    /// <summary>
    /// Gets a specific chat message.
    /// </summary>
    /// <param name="id">ID of the chat message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The chat message.</returns>
    public async Task<ChatMessage?> GetMessageAsync(Guid id, CancellationToken ct = default) =>
        await _messagingPersistance.GetMessageAsync(id, ct);
}