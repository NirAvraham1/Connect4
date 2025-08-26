using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ConnectFourWeb.Models
{
    public class Move
    {
        public int Id { get; set; }

        public int GameId { get; set; }

        [ForeignKey("GameId")]
        [JsonIgnore]
        public Game? Game { get; set; }

        public int Column { get; set; }
        public int Row { get; set; }
        public bool IsPlayer { get; set; }
        public int TurnNumber { get; set; }
    }
}
