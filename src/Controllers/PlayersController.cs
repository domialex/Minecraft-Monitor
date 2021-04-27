using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minecraft_Monitor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Minecraft_Monitor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly MinecraftMonitorContext minecraftMonitorContext;
        public PlayersController(MinecraftMonitorContext minecraftMonitorContext)
        {
            this.minecraftMonitorContext = minecraftMonitorContext;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IList<Player>>> GetAllAsync()
        {
            var players = await minecraftMonitorContext.Players.ToListAsync();

            return players;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Player>> GetByIdAsync(Guid id)
        {
            var player = await minecraftMonitorContext.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return player;
        }
    }
}