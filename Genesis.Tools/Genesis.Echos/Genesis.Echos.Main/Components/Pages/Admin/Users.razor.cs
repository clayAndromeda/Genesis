using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Domain.Enums;
using Genesis.Echos.Main.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Genesis.Echos.Main.Components.Pages.Admin;

public partial class Users : ComponentBase
{
    [Inject] private AdminService AdminService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private List<ApplicationUser>? users;
    private string? errorMessage;
    private string? successMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
    }

    private async Task LoadUsers()
    {
        try
        {
            users = await AdminService.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"ユーザーの読み込みに失敗しました: {ex.Message}";
        }
    }

    private async Task ChangeRole(string userId, UserRole newRole)
    {
        try
        {
            errorMessage = null;
            successMessage = null;

            var result = await AdminService.ChangeUserRoleAsync(userId, newRole);
            if (result)
            {
                successMessage = $"ロールを{GetRoleDisplayName(newRole)}に変更しました。";
                await LoadUsers();
            }
            else
            {
                errorMessage = "ロールの変更に失敗しました。";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"エラーが発生しました: {ex.Message}";
        }
    }

    private async Task DeleteUser(string userId, string email)
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm",
            $"ユーザー {email} を削除してもよろしいですか?この操作は取り消せません。"))
        {
            return;
        }

        try
        {
            errorMessage = null;
            successMessage = null;

            var result = await AdminService.DeleteUserAsync(userId);
            if (result)
            {
                successMessage = $"ユーザー {email} を削除しました。";
                await LoadUsers();
            }
            else
            {
                errorMessage = "ユーザーの削除に失敗しました。管理者は削除できません。";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"エラーが発生しました: {ex.Message}";
        }
    }

    private string GetRoleBadgeClass(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "bg-danger",
            UserRole.Leader => "bg-primary",
            UserRole.Member => "bg-secondary",
            _ => "bg-secondary"
        };
    }

    private string GetRoleDisplayName(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "管理者",
            UserRole.Leader => "リーダー",
            UserRole.Member => "ゲスト",
            _ => "不明"
        };
    }
}
