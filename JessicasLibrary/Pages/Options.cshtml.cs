using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JessicasLibrary.Pages
{
    [Authorize]
    public class OptionsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public OptionsModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty] public string FirstName { get; set; } = "";
        [BindProperty] public string LastName { get; set; } = "";
        [BindProperty] public string Email { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User)!;
            Email = user.Email!;

            var claims = await _userManager.GetClaimsAsync(user);
            FirstName = claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "";
            LastName = claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User)!;
            var claims = await _userManager.GetClaimsAsync(user);

            // 1) Update email if changed
            if (Email != user.Email)
            {
                var setEmail = await _userManager.SetEmailAsync(user, Email);
                if (!setEmail.Succeeded)
                    ModelState.AddModelError("", "Email update failed");
            }

            // 2) Update given_name claim
            var givenClaim = claims.FirstOrDefault(c => c.Type == "given_name");
            if (givenClaim != null)
                await _userManager.RemoveClaimAsync(user, givenClaim);

            if (!string.IsNullOrWhiteSpace(FirstName))
                await _userManager.AddClaimAsync(
                    user,
                    new Claim("given_name", FirstName)
                );

            // 3) Update family_name claim
            var familyClaim = claims.FirstOrDefault(c => c.Type == "family_name");
            if (familyClaim != null)
                await _userManager.RemoveClaimAsync(user, familyClaim);

            if (!string.IsNullOrWhiteSpace(LastName))
                await _userManager.AddClaimAsync(
                    user,
                    new Claim("family_name", LastName)
                );

            // 4) Update password if provided
            if (!string.IsNullOrWhiteSpace(Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var pwdResult = await _userManager.ResetPasswordAsync(user, token, Password);
                if (!pwdResult.Succeeded)
                    ModelState.AddModelError("", "Password update failed");
            }

            // 5) Refresh sign-in so cookie picks up new claims
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToPage();
        }
    }
}
