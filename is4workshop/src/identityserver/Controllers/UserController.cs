using System.Threading.Tasks;
using IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        [HttpGet]
        public ViewResult Register()
        {
            return View("register");
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromForm] Registration registration)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = registration.Username
                };

                var createResult = await _userManager.CreateAsync(user, registration.Password);

                if (createResult.Succeeded)
                    return RedirectToAction("Index", "Home");

                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View("register");
        }
    }
}