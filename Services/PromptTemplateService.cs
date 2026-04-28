using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PromptTemplateService : IPromptTemplateService
{
    private readonly ConcurrentDictionary<string, PromptTemplate> _templates = new();

    public async Task AddTemplateAsync(PromptTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        _templates.TryAdd(template.Id, template);
    }

    public async Task<PromptTemplate> GetTemplateByIdAsync(string id)
    {
        _templates.TryGetValue(id, out var template);
        return template;
    }

    public async Task<IEnumerable<PromptTemplate>> GetTemplatesByUserAsync(string userId)
    {
        return _templates.Values.Where(t => t.UserId == userId);
    }

    public async Task<IEnumerable<PromptTemplate>> GetPublicTemplatesAsync()
    {
        return _templates.Values.Where(t => t.IsPublic);
    }

    public async Task<bool> UpdateTemplateAsync(PromptTemplate template)
    {
        if (_templates.ContainsKey(template.Id))
        {
            template.UpdatedAt = DateTime.UtcNow;
            _templates[template.Id] = template;
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteTemplateAsync(string id)
    {
        return _templates.TryRemove(id, out _);
    }
}