using ConnectFourWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectFourWeb.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        public UsersController(ApplicationDbContext ctx) => _ctx = ctx;

        public sealed class UserLiteDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = "";
            public int Identifier { get; set; }
            public string Country { get; set; } = "";
        }

        // GET /api/users/by-identifier/123
        [HttpGet("by-identifier/{identifier:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<UserLiteDto>> GetByIdentifier(int identifier)
        {
            var dto = await _ctx.Users
                .AsNoTracking()
                .Where(u => u.Identifier == identifier)
                .Select(u => new UserLiteDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Identifier = u.Identifier,
                    Country = u.Country
                })
                .FirstOrDefaultAsync();

            if (dto == null) return NotFound();
            return Ok(dto);
        }
    }
}
