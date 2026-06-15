using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Pokekool.API.Controllers;

/// <summary>
/// Base de tous les controllers : versionné /api/v{version}, ApiController, ProducesJson.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
}
