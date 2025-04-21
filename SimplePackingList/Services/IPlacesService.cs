using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimplePackingList.Services;

public interface IPlacesService
{
    Task<List<string>> GetPlaceSuggestionsAsync(string query, CancellationToken cancellationToken);
}
