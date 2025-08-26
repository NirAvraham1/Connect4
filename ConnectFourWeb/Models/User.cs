using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ConnectFourWeb.Models
{
    [Index(nameof(Identifier), IsUnique = true)]
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        [Required]
        public string Country { get; set; } = "";

        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits")]
        public string Phone { get; set; } = "";

        [Range(1, 1000, ErrorMessage = "The identifier must be between 1-1000")]
        public int Identifier { get; set; }
    }
}
