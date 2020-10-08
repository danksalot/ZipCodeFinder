using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZipCodeFinder.Services
{
    public interface IZipCodeService
    {
        Task<List<string>> LookupZipCodes(string city, string state);
    }
}