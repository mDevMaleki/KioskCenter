using KioskCenter.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class InfoController : ControllerBase
{
    private const string LicensePath = "license.dat";

    private readonly HardwareService _hardwareService;
    private readonly LicenseValidator _validator;
    private readonly LicenseManager _licenseManager;

    public InfoController(HardwareService hardwareService, LicenseValidator validator, LicenseManager licenseManager)
    {
        _hardwareService = hardwareService;
        _validator = validator;
        _licenseManager = licenseManager;
    }

    [HttpGet("hardware-id")]
    public IActionResult GetHardwareId()
    {
        return Ok(new { HardwareHash = _hardwareService.GetHardwareHash() });
    }

    [HttpGet("license-status")]
    public IActionResult GetLicenseStatus()
    {
        return Ok(new
        {
            licensed = _licenseManager.IsLicensed,
            message = _licenseManager.StatusMessage,
            hardwareHash = _hardwareService.GetHardwareHash()
        });
    }

    [HttpPost("upload-license")]
    public async Task<IActionResult> UploadLicense(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "فایلی انتخاب نشده است" });

        var tempPath = "license_uploaded_tmp.dat";

        try
        {
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = _validator.Validate(tempPath);
            if (!result.IsValid)
                return BadRequest(new { success = false, message = result.Message });

            System.IO.File.Copy(tempPath, LicensePath, overwrite: true);

            var revalidated = await _licenseManager.ValidateOnStartup();
            if (!revalidated)
                return BadRequest(new { success = false, message = _licenseManager.StatusMessage });

            return Ok(new { success = true, message = "لایسنس با موفقیت فعال شد" });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }
}
