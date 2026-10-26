using Microsoft.AspNetCore.Mvc;

namespace tickets_core.src.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase;
