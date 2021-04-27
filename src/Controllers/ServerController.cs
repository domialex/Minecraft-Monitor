using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minecraft_Monitor.Models;
using System.Threading.Tasks;

namespace Minecraft_Monitor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : ControllerBase
    {
        private readonly MinecraftMonitorContext minecraftMonitorContext;
        public ServerController(MinecraftMonitorContext minecraftMonitorContext)
        {
            this.minecraftMonitorContext = minecraftMonitorContext;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServerInfo>> GetAllAsync()
        {
            return await minecraftMonitorContext.ServerInfo.SingleAsync();
        }
    }
}