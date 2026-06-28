using System.ComponentModel.DataAnnotations;
using KioskCenter.Services;

namespace KioskCenter.Models
{
    public class PosDevice
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = ""; // مثلاً "پارسیان", "پرداخت نوین"
        public PosType Type { get; set; }  // "Parsian" یا "PardakhtNovin"
        public string IpAddress { get; set; } = "";
        public int Port { get; set; } = 1362;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int Priority { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}