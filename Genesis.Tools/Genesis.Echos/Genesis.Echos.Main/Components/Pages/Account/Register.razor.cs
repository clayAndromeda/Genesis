using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Genesis.Echos.Main.Components.Pages.Account;

public partial class Register
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromForm]
    private RegisterModel? model { get; set; }

    private bool isSubmitting = false;
    private string? errorMessage;
    private string? successMessage;

    protected override void OnInitialized()
    {
        model ??= new();
    }

    private async Task HandleRegister()
    {
        if (model == null) return;

        try
        {
            isSubmitting = true;
            errorMessage = null;
            successMessage = null;

            // パスワード確認チェック
            if (model.Password != model.ConfirmPassword)
            {
                errorMessage = "パスワードが一致しません。";
                return;
            }

            // メールアドレス重複チェック
            var existingUser = await UserManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                errorMessage = "このメールアドレスは既に登録されています。";
                return;
            }

            // 新規ユーザー作成
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                Role = UserRole.Member, // 全員Memberロールを付与
                CreatedAt = DateTime.UtcNow
            };

            var result = await UserManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Memberロールを付与
                await UserManager.AddToRoleAsync(user, "Member");

                successMessage = "アカウント登録が完了しました。ログインページにリダイレクトします...";

                // 自動ログイン
                await SignInManager.SignInAsync(user, isPersistent: false);

                // 2秒待ってから投稿一覧にリダイレクト
                await Task.Delay(2000);
                Navigation.NavigateTo("/posts", forceLoad: true);
            }
            else
            {
                // エラーメッセージを構築
                errorMessage = "登録に失敗しました: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"登録中にエラーが発生しました: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "メールアドレスは必須です")]
        [EmailAddress(ErrorMessage = "有効なメールアドレスを入力してください")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "パスワードは必須です")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "パスワードは8文字以上で入力してください")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "パスワード（確認）は必須です")]
        [Compare(nameof(Password), ErrorMessage = "パスワードが一致しません")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
