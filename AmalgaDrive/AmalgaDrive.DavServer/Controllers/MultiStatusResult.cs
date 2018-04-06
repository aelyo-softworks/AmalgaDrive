using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AmalgaDrive.DavServer.Controllers
{
    public class MultiStatusResult : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            await Task.CompletedTask;
        }
    }
}
