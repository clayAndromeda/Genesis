using Genesis.Echos.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Genesis.Echos.Main.Components.Pages.Account;

public partial class Logout
{
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool isLoggingOut = true;

    protected override async Task OnInitializedAsync()
    {
        await SignInManager.SignOutAsync();
        isLoggingOut = false;

        // 2秒後にホームページにリダイレクト
        await Task.Delay(2000);
        Navigation.NavigateTo("/", forceLoad: true);
    }
}
