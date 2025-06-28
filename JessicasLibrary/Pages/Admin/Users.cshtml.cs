using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JessicasLibrary.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>All users in the system</summary>
        public IList<IdentityUser> Users { get; private set; } = new List<IdentityUser>();

        /// <summary>Mapping from userId → their role names</summary>
        public Dictionary<string, IList<string>> UserRoles { get; private set; }
            = new Dictionary<string, IList<string>>();

        public async Task<IActionResult> OnGetAsync()
        {
            Users = _userManager.Users.ToList();
            foreach (var u in Users)
            {
                UserRoles[u.Id] = await _userManager.GetRolesAsync(u);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostPromoteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDemoteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // prevent self-deletion
                if (user.UserName == User.Identity?.Name)
                {
                    ModelState.AddModelError("", "You cannot delete your own account.");
                }
                else
                {
                    await _userManager.DeleteAsync(user);
                }
            }
            return RedirectToPage();
        }
    }
}
