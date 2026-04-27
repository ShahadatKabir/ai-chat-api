using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IFavoritesService
{
    Task AddFavoriteAsync(string userId, string messageId, string note = "");
    Task RemoveFavoriteAsync(string userId, string messageId);
    Task<List<Favorite>> GetUserFavoritesAsync(string userId);
    Task<bool> IsFavoriteAsync(string userId, string messageId);
}

public class FavoritesService : IFavoritesService
{
    private readonly ConcurrentDictionary<string, List<Favorite>> _userFavorites = new();

    public async Task AddFavoriteAsync(string userId, string messageId, string note = "")
    {
        var favorites = _userFavorites.GetOrAdd(userId, new List<Favorite>());
        if (!favorites.Any(f => f.MessageId == messageId))
        {
            favorites.Add(new Favorite
            {
                UserId = userId,
                MessageId = messageId,
                Note = note
            });
        }
    }

    public async Task RemoveFavoriteAsync(string userId, string messageId)
    {
        if (_userFavorites.TryGetValue(userId, out var favorites))
        {
            favorites.RemoveAll(f => f.MessageId == messageId);
        }
    }

    public async Task<List<Favorite>> GetUserFavoritesAsync(string userId)
    {
        return _userFavorites.GetOrAdd(userId, new List<Favorite>());
    }

    public async Task<bool> IsFavoriteAsync(string userId, string messageId)
    {
        if (_userFavorites.TryGetValue(userId, out var favorites))
        {
            return favorites.Any(f => f.MessageId == messageId);
        }
        return false;
    }
}