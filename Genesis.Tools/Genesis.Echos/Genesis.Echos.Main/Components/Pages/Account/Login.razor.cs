using Genesis.Echos.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Genesis.Echos.Main.Components.Pages.Account;

public partial class Login
{
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromForm]
    private LoginModel? model { get; set; }

    private bool isSubmitting = false;
    private string? errorMessage;

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    protected override void OnInitialized()
    {
        model ??= new();
    }

    private async Task HandleLogin()
    {
        if (model == null) return;

        try
        {
            isSubmitting = true;
            errorMessage = null;

            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                errorMessage = "メールアドレスまたはパスワードが正しくありません。";
                return;
            }

            var result = await SignInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                Navigation.NavigateTo(ReturnUrl ?? "/posts", forceLoad: true);
            }
            else
            {
                errorMessage = "メールアドレスまたはパスワードが正しくありません。";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"ログイン中にエラーが発生しました: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "メールアドレスは必須です")]
        [EmailAddress(ErrorMessage = "有効なメールアドレスを入力してください")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "パスワードは必須です")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
