using KioskCenter.Services;
using Microsoft.AspNetCore.Mvc;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnlinePayController : ControllerBase
    {
        private readonly OnlinePayService _onlinePayService;

        public OnlinePayController(OnlinePayService onlinePayService)
        {
            _onlinePayService = onlinePayService;
        }

        [HttpGet("{Amount}")]
        public async Task<IActionResult> SendAmount(decimal Amount)
        {
            var qrImage = await _onlinePayService.CreatePaymentQr(Amount);
            return File(qrImage, "image/png");
        }
    }
}