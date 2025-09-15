using System.Security.Authentication;
using Mapster;
using MapsterMapper;
using Chat.Api.Infrastructure.Authentication;
using Chat.ApiModel.Messaging;
using Chat.Business.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Chat.Business.Persistance;

namespace Chat.Api.Hubs;

/// <summary>
/// Represents the company messaging hub.
/// </summary>
[Authorize]
public sealed class MessagingHub : Hub<IMessagingHubPush>, IMessagingHubInvoke
{
    /// route to the hub
    public static string HubPath => "/api/hub/messaging";

    private readonly MessagingService _messagingService;
    private readonly ChatRoomsService _chatRoomsService;   
    private readonly IMapper _mapper;

    private readonly IUserPersistance _userPersistance;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingHub"/> class.
    /// </summary>
    public MessagingHub(MessagingService messagingService, ChatRoomsService chatRoomsService, IMapper mapper, IUserPersistance userPersistance)
    {
        _messagingService = messagingService;
        _chatRoomsService = chatRoomsService;
        _mapper = mapper;
        _userPersistance = userPersistance;
    }

    private const int MaxJoinedRooms = 5;

    private string NameIdentifier => Context.User?.GetNameIdentifier()
        ?? throw new AuthenticationException("User nameidentifier not found in Claims.");

    /// <summary>
    /// Gets the chat room from an offer.
    /// </summary>
    public async Task<ChatRoomDto> GetChatRoom(Guid roomId)
    {
        var room = await _messagingService.GetChatRoom(roomId)
                   ?? throw new ArgumentException("Chat room not found.");

        return _mapper.Map<ChatRoomDto>(room);
    }

    /// <summary>
    /// Récupère la liste complète de toutes les chatrooms existantes.
    /// </summary>
    /// <returns>Une collection de toutes les chatrooms.</returns>
    public async Task<IEnumerable<ChatRoomDto>> GetAllChatRooms()
    {
        var roomsList = await _chatRoomsService.GetAllAsync(); 
        
        return _mapper.Map<IEnumerable<ChatRoomDto>>(roomsList);
    }

    /// <summary>
    /// Creates a new chat room with the given name and broadcasts the creation.
    /// </summary>
    public async Task<ChatRoomDto> CreateChatRoomWithName(string name)
    {
        var room = await _chatRoomsService.CreateAsync(name);
        var dto  = _mapper.Map<ChatRoomDto>(room);
        await Clients.All.ChatRoomCreated(dto);
        return dto;
    }

    /// <summary>
    /// Crée une chatroom avec un nom par défaut lorsque aucun nom n’est fourni.
    /// </summary>
    /// <returns>La chatroom créée.</returns>
    public async Task<ChatRoomDto> CreateChatRoom()
    {
        // valeur par défaut
        var room = await _chatRoomsService.CreateAsync("General");
        var dto  = _mapper.Map<ChatRoomDto>(room);
        await Clients.All.ChatRoomCreated(dto);
        return dto;
    }


    /// <inheritdoc />
    public async Task<IEnumerable<ChatMessageDto>> JoinChatRoom(Guid roomId)
    {
        var nameIdentifier = NameIdentifier;

        var user = await _userPersistance.GetUserByUsernameAsync(nameIdentifier)
            ?? throw new HubException("Utilisateur non trouvé.");
        
        var userIdAsGuid = user.Id;
        
        var userRoomCount = await _chatRoomsService.GetUserRoomsCountAsync(userIdAsGuid);
        
        if (userRoomCount >= MaxJoinedRooms)
        {
            throw new HubException($"Limite atteinte. Vous ne pouvez pas rejoindre plus de {MaxJoinedRooms} salons.");
        }
        
        if (await _messagingService.GetChatRoomAsync(roomId, Context.ConnectionAborted) is not { } room)
            throw new KeyNotFoundException("Chatroom not found.");

        await _chatRoomsService.AddParticipantAsync(roomId, userIdAsGuid);

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

        var messages = _messagingService.GetMessagesInRoom(roomId);
        return messages.Adapt<IEnumerable<ChatMessageDto>>(_mapper.Config);
    }

    /// <inheritdoc />
    public async Task LeaveChatRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    /// <inheritdoc />
    public async Task SendMessage(string roomId, string message)
    {
        await _messagingService.SubmitMessageAsync(roomId, message, NameIdentifier);
    }
}