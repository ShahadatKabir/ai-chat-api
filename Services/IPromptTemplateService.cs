using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPromptTemplateService
{
    Task AddTemplateAsync(PromptTemplate template);
    Task<PromptTemplate> GetTemplateByIdAsync(string id);
    Task<IEnumerable<PromptTemplate>> GetTemplatesByUserAsync(string userId);
    Task<IEnumerable<PromptTemplate>> GetPublicTemplatesAsync();
    Task<bool> UpdateTemplateAsync(PromptTemplate template);
    Task<bool> DeleteTemplateAsync(string id);
}