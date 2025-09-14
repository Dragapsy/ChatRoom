using System.Threading;
using System.Threading.Tasks;
using Chat.Business.Persistance;
using Chat.Model.Messaging;

namespace Chat.Business.Messaging
{
    /// <summary>
    /// Service métier pour la gestion des chatrooms (tâche 1 : création).
    /// </summary>
    public sealed class ChatRoomsService
    {
        private readonly IChatRoomsPersistance _repo;

        /// <summary>
        /// Initialise une nouvelle instance du service de chatrooms.
        /// </summary>
        /// <param name="repo">Dépôt de persistance des chatrooms.</param>
        public ChatRoomsService(IChatRoomsPersistance repo) => _repo = repo;

        /// <summary>
        /// Valide le nom puis crée une chatroom en base.
        /// </summary>
        /// <param name="name">Nom lisible de la chatroom.</param>
        /// <param name="ct">Jeton d'annulation.</param>
        /// <returns>La chatroom créée.</returns>
        /// <exception cref="System.ArgumentException">
        /// Levée si <paramref name="name"/> est nul, vide ou fait moins de 2 caractères.
        /// </exception>
        public Task<ChatRoom> CreateAsync(string name, CancellationToken ct = default)
        {
            var trimmed = (name ?? string.Empty).Trim();
            if (trimmed.Length < 2)
                throw new System.ArgumentException("Room name must be at least 2 characters.", nameof(name));

            return _repo.CreateAsync(trimmed, ct);
        }

        /// <summary>
        /// Récupère toutes les chatrooms.
        /// </summary>
        public Task<IEnumerable<ChatRoom>> GetAllAsync(CancellationToken ct = default)
        {
            return _repo.GetAllAsync(ct);
        }
    }
}
