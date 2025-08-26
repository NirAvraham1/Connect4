using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectFourWeb.Models
{
    public class Game
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Result { get; set; } = "";

        public TimeSpan Duration => EndTime - StartTime;

        public ICollection<Move> Moves { get; set; } = new List<Move>();
    }
}
