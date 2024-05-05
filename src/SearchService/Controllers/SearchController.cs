using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Entities;
using SearchService.Models;
using ZstdSharp.Unsafe;

namespace SearchService;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
[HttpGet]
public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchparams) {
    var query = DB.PagedSearch<Item, Item>();
    
    Console.WriteLine("LOOKING FOR CARS " + searchparams.searchTerm);
    if (!string.IsNullOrEmpty(searchparams.searchTerm)) {
        query.Match(Search.Full, searchparams.searchTerm).SortByTextScore();
    }

    query = searchparams.OrderBy switch
    {
        "make" => query.Sort(x => x.Ascending(a => a.Make)) ,
        "new" => query.Sort(x => x.Descending(a => a.CreatedAt)) ,
        _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
    };

    query = searchparams.FilterBy switch
    {
        "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
        "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) 
        && x.AuctionEnd > DateTime.UtcNow),
        _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow)
    };

    if (!string.IsNullOrEmpty(searchparams.Seller)) {
        query.Match(x => x.Seller == searchparams.Seller);
    }

    if (!string.IsNullOrEmpty(searchparams.Winner)) {
        query.Match(x => x.Winner == searchparams.Winner);
    }

    query.PageNumber(searchparams.PageNumber);
    query.PageSize(searchparams.PageSize);  

    var result = await query.ExecuteAsync();
    return Ok(new 
    {results = result.Results,
    pageCount = result.PageCount,
    totalCount = result.TotalCount
    }
    );
}
}
