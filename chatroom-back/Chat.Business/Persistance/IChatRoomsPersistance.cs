namespace Chat.Business.Persistance;

using System.Threading;
using System.Threading.Tasks;
using Chat.Model.Messaging;

/// <summary>Accès persistance pour les chatrooms (création minimale).</summary>
public interface IChatRoomsPersistance
{
    /// <summary>Crée une nouvelle chatroom et persiste les changements.</summary>
    Task<ChatRoom> CreateAsync(string name, CancellationToken ct = default);

    /// <summary>Gets all rooms</summary>
    Task<IEnumerable<ChatRoom>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Gets all rooms count</summary>
    Task<int> GetUserRoomsCountAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Adding participant</summary>
    Task AddParticipantAsync(Guid roomId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Récupère l'historique des messages d'un salon, triés par date.
    /// </summary>
    /// <param name="roomId">L'ID du salon.</param>
    /// <param name="ct">Le jeton d'annulation.</param>
    /// <returns>Une collection de messages du salon.</returns>
    Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(Guid roomId, CancellationToken ct = default);

    /// <summary>
    /// Récupère un salon de discussion par son ID, incluant ses participants.
    /// </summary>
    Task<ChatRoom?> GetChatRoomAsync(Guid roomId, CancellationToken ct = default);

}
