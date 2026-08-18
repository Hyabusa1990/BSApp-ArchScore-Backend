using Fawkes.Api.Authentication;

namespace Fawkes.Api.Store
{


    public interface IFawkesDataStore
    {
    }

    public class FawkesDataStore(FawkesDbContext context) : IFawkesDataStore
    {



    }
}
