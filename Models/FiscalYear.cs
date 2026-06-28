using System.ComponentModel.DataAnnotations;

namespace KioskCenter.Models
{
    public class FiscalYear
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // مثلاً "1404"

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsClosed { get; set; } = false;

        public DateTime? ClosedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
