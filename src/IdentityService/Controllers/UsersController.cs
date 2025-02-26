using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly ISearchService _searchService;

        public UsersController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        // GET: api/users/search/username/{username}
        [HttpGet("search/username/{username}")]
        public async Task<IActionResult> SearchUsersByUsername(string username)
        {
            var user = await _searchService.SearchUsersByUsernameAsync(username);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        // GET: api/users/search/id/{userId}
        [HttpGet("search/id/{userId:int}")]
        public async Task<IActionResult> SearchUsersByUserId(int userId)
        {
            var user = await _searchService.SearchUsersByUserIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }
    }
}
