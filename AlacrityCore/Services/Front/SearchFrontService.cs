using AlacrityCore.Enums;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Front;

public interface ISearchFrontService
{
    Task<List<InstrumentBriefDto>> SearchInstruments(string searchTerm);
}

internal class SearchFrontService : ISearchFrontService
{
    private readonly ISearchQuery _searchQuery;
    public SearchFrontService(ISearchQuery searchQuery) => (_searchQuery) = (searchQuery);

    public async Task<List<InstrumentBriefDto>> SearchInstruments(string searchTerm)
        => (await _searchQuery.SearchInstruments(searchTerm))
            .Where(s => s.InstrumentId != (long)SpecialInstruments.Cash)
            .ToList();
}
