using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PollyTutorial.Services;

namespace PollyTutorial.Controllers
{
    [ApiController]
    public class GithubController : ControllerBase
    {
        private readonly IGithubService _githubService;

        public GithubController(IGithubService githubService)
        {
            this._githubService = githubService;
        }

        [HttpGet("users/{userName}")]
        public async Task<IActionResult> GetUserByUserName(string userName)
        {
            var user = await _githubService.GetUserByUserNameAsync(userName);
            return user != null ? (IActionResult)Ok(user) : NotFound();
        }
    }
}
