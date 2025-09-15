using Chat.Business.Persistance;
using Chat.Model.Messaging;
using Microsoft.EntityFrameworkCore; 

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

        /// <summary>
        /// Recupere tout les room <see cref="ChatRoomsRepository"/>.
        /// </summary>
        public async Task<IEnumerable<ChatRoom>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.ChatRooms
                .AsNoTracking()
                .ToListAsync(ct);
        }

        /// <summary>
        /// Recupere tout le nombre des rooms/>.
        /// </summary>
        public async Task<int> GetUserRoomsCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.ChatRooms
            .CountAsync(room => room.Participants.Any(p => p.Id == userId), ct);
    }

        /// <summary>Adding participant</summary>
        public async Task AddParticipantAsync(Guid roomId, Guid userId, CancellationToken ct = default)
        {
            var room = await _db.ChatRooms.Include(r => r.Participants).FirstOrDefaultAsync(r => r.Id == roomId, ct);
            var user = await _db.Users.FindAsync(new object[] { userId }, ct);

            if (room != null && user != null && !room.Participants.Any(p => p.Id == userId))
            {
                room.Participants.Add(user);
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
