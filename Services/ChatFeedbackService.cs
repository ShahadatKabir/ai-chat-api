using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ChatFeedbackService : IChatFeedbackService
{
    private readonly ConcurrentDictionary<string, ChatFeedback> _feedbacks = new();

    public async Task AddFeedbackAsync(ChatFeedback feedback)
    {
        _feedbacks.TryAdd(feedback.Id, feedback);
    }

    public async Task<IEnumerable<ChatFeedback>> GetFeedbackByUserAsync(string userId)
    {
        return _feedbacks.Values.Where(f => f.UserId == userId);
    }

    public async Task<double> GetAverageRatingAsync(string userId)
    {
        var userFeedbacks = _feedbacks.Values.Where(f => f.UserId == userId);
        if (!userFeedbacks.Any())
            return 0;
        return userFeedbacks.Average(f => f.Rating);
    }
}