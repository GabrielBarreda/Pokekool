using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

/// <summary>
/// Endpoint témoin de la pipeline versionnée. À retirer une fois les vrais modules en place.
/// </summary>
public sealed class PingController : BaseApiController
{
    [HttpGet]
    [SwaggerOperation(Summary = "Ping versionné", Description = "Valide que la route /api/v1/ping est exposée.")]
    public IActionResult Get() => Ok(new { message = "pong", version = "v1" });
}
