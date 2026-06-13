using KioskCenter.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class InfoController : ControllerBase
{
    private readonly HardwareService _hardwareService;

    public InfoController(HardwareService hardwareService) => _hardwareService = hardwareService;

    [HttpGet("hardware-id")]
    public IActionResult GetHardwareId()
    {
        return Ok(new { HardwareHash = _hardwareService.GetHardwareHash() });
    }
}
