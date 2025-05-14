using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthConnector.Models
{
    [Table("Logs")]  // Specifies that this model maps to the "Logs" table in the database
    public class LogModel
    {
        [Key]
        public int LogID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? LogLevel { get; set; }
        public string? Message { get; set; }
    }
}
