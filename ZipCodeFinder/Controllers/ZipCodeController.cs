using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZipCodeFinder.Services;

namespace ZipCodeFinder.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ZipCodeController : ControllerBase
    {
        IZipCodeService _zipCodeService;

        public ZipCodeController(IZipCodeService zipCodeService)
        {
            _zipCodeService = zipCodeService;
        }

        [HttpGet]
        [ResponseCache(VaryByQueryKeys = new string[] {"city", "state"}, Duration = 86400)]
        public async Task<IEnumerable<string>> GetZipCodes([FromQuery] string city, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(state))
            {
                return new List<string>();
            }

            return await _zipCodeService.LookupZipCodes(city, state);
        }
    }
}
