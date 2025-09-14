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
}
