using System.Collections.Generic;
using System.Threading.Tasks;

public interface IChatFeedbackService
{
    Task AddFeedbackAsync(ChatFeedback feedback);
    Task<IEnumerable<ChatFeedback>> GetFeedbackByUserAsync(string userId);
    Task<double> GetAverageRatingAsync(string userId);
}