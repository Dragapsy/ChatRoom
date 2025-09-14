using Chat.Business.Persistance;
using Chat.Model.Messaging;

namespace Chat.Repository.Repositories
{
    /// <summary>
    /// Dépôt de persistance pour les chatrooms.
    /// </summary>
    public sealed class ChatRoomsRepository : IChatRoomsPersistance
    {
        private readonly PlatformDbContext _db;

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="ChatRoomsRepository"/>.
        /// </summary>
        /// <param name="db">Contexte base de données de la plateforme.</param>
        public ChatRoomsRepository(PlatformDbContext db) => _db = db;

        /// <inheritdoc />
        public async Task<ChatRoom> CreateAsync(string name, CancellationToken ct = default)
        {
            var room = new ChatRoom { Name = name };
            _db.ChatRooms.Add(room);
            await _db.SaveChangesAsync(ct);
            return room;
        }
    }
}
