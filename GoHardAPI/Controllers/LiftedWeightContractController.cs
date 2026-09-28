using Asp.Versioning;
using GoHardAPI.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class LiftedWeightContractController : ControllerBase
    {
        private readonly IOptionsMonitor<LiftedWeightOptions> _options;
        public LiftedWeightContractController(IOptionsMonitor<LiftedWeightOptions> options) => _options = options;

        /// <summary>Whether stored workout history is canonical kg (production reset verified).</summary>
        [HttpGet]
        public ActionResult<object> Get() => Ok(new { canonicalHistory = _options.CurrentValue.CanonicalHistory });
    }
}
