using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TournamentPlanner.Data;

namespace TournamentPlanner.Controllers
{
    [ApiController]
    [Route("api/players")]
    public class PlayersController : ControllerBase
    {
        private readonly TournamentPlannerDbContext _context;

        public PlayersController(TournamentPlannerDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IEnumerable<Player>> GetPlayers([FromQuery] string nameFilter)
        {
            return await _context.GetFilteredPlayers(nameFilter);
        }
    }
}
