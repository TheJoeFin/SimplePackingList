using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimplePackingList.Services;

public interface IPlacesService
{
    Task<List<PlacePrediction>> GetPlaceSuggestionsAsync(string query, CancellationToken cancellationToken);
}
