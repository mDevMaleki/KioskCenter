using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KioskCenter.Authorization;
using KioskCenter.Interfaces;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PosDeviceController : ControllerBase
    {
        private readonly IPosManagerService _posManager;
        private readonly ILogger<PosDeviceController> _logger;

        public PosDeviceController(IPosManagerService posManager, ILogger<PosDeviceController> logger)
        {
            _posManager = posManager;
            _logger = logger;
        }

        // دریافت لیست تمام دستگاه‌های POS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var devices = await _posManager.GetAllDevices();
            return Ok(devices);
        }

        // دریافت یک دستگاه POS
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var device = await _posManager.GetDevice(id);
            if (device == null)
                return NotFound(new { success = false, message = "دستگاه یافت نشد" });
            return Ok(device);
        }

        // اضافه کردن دستگاه POS جدید
        [Authorize, RequirePermission("pos-devices")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PosDevice device)
        {
            try
            {
                var result = await _posManager.AddDevice(device);
                return Ok(new { success = true, message = "دستگاه با موفقیت اضافه شد", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ویرایش دستگاه POS
        [Authorize, RequirePermission("pos-devices")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PosDevice device)
        {
            if (id != device.Id)
                return BadRequest(new { success = false, message = "شناسه نامعتبر" });

            try
            {
                var result = await _posManager.UpdateDevice(device);
                return Ok(new { success = true, message = "دستگاه با موفقیت ویرایش شد", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // حذف دستگاه POS
        [Authorize, RequirePermission("pos-devices")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _posManager.DeleteDevice(id);
            if (!result)
                return NotFound(new { success = false, message = "دستگاه یافت نشد" });
            return Ok(new { success = true, message = "دستگاه با موفقیت حذف شد" });
        }

        // انجام پرداخت
        [HttpPost("pay")]
        public async Task<IActionResult> Pay([FromBody] PosPayRequest request)
        {
            var result = await _posManager.SendPayment(request.Amount, request.DeviceId);
            return Ok(result);
        }

        // بررسی اتصال
        [HttpPost("check-connection")]
        public async Task<IActionResult> CheckConnection([FromBody] PosCheckRequest request)
        {
            var result = await _posManager.CheckConnection(request.DeviceId);
            return Ok(result);
        }

        // دریافت دستگاه فعال
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var device = await _posManager.GetActiveDevice();
            if (device == null)
                return NotFound(new { success = false, message = "هیچ دستگاه فعالی یافت نشد" });
            return Ok(device);
        }
    }

    public class PosPayRequest
    {
        public int Amount { get; set; }
        public int? DeviceId { get; set; }
    }

    public class PosCheckRequest
    {
        public int? DeviceId { get; set; }
    }
}