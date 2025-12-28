using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Main.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Genesis.Echos.Main.Components.Pages.Posts;

public partial class Detail
{
    [Inject] private PostService PostService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    private Post? post;
    private bool isLoading = true;
    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            post = await PostService.GetPostByIdAsync(Id);

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading post: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private bool IsAuthor(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId == post?.AuthorId;
    }

    private async Task DeletePost()
    {
        if (post == null || string.IsNullOrEmpty(currentUserId)) return;

        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "この投稿を削除しますか？");
        if (!confirmed) return;

        try
        {
            var success = await PostService.DeletePostAsync(post.Id, currentUserId);
            if (success)
            {
                Navigation.NavigateTo("/posts");
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", "投稿の削除に失敗しました。");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"エラーが発生しました: {ex.Message}");
        }
    }
}
