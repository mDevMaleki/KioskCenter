using KioskCenter.Services;
using Microsoft.AspNetCore.Mvc;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZarinpalCallbackController : ControllerBase
    {
        private readonly OnlinePayService _onlinePayService;

        public ZarinpalCallbackController(OnlinePayService onlinePayService)
        {
            _onlinePayService = onlinePayService;
        }
        [HttpGet("get-authority/{amount}")]
        public async Task<IActionResult> GetAuthority(decimal amount)
        {
            try
            {
                var authority = await _onlinePayService.CreatePaymentAndGetAuthority(amount);
                return Ok(new { authority });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("pay/{amount}")]
        public async Task<IActionResult> CreatePayment(decimal amount)
        {
            try
            {
                var qrImage = await _onlinePayService.CreatePaymentQr(amount);
                return File(qrImage, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyPayment([FromQuery] decimal amount, [FromQuery] string authority, [FromQuery] int orderId)
        {
            try
            {
                var result = await _onlinePayService.VerifyPayment(amount, authority);

                if (result.IsSuccess)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "پرداخت با موفقیت تایید شد",
                        refId = result.RefId,
                        cardPan = result.CardPan,
                        orderId
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        code = result.Code
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}