using AlacrityCore.Models.ReqRes.Search;
using AlacrityCore.Services.Front;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("search")]
public class SearchController
{
    private readonly ISearchFrontService _searchFrontService;
    public SearchController(ISearchFrontService searchFrontService) => (_searchFrontService) = (searchFrontService);

    [HttpGet]
    public async Task<SearchInstrumentsResponse> SearchInstruments([FromQuery] SearchInstrumentsRequest request)
        => new()
        {
            Instruments = await _searchFrontService.SearchInstruments(request.SearchTerm)
        };
}
