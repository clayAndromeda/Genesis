using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Main.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Genesis.Echos.Main.Components.Pages.Admin;

public partial class Posts : ComponentBase
{
    [Inject] private PostService PostService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private List<Post>? posts;
    private string? errorMessage;
    private string? successMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadPosts();
    }

    private async Task LoadPosts()
    {
        try
        {
            posts = await PostService.GetAllPostsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"投稿の読み込みに失敗しました: {ex.Message}";
        }
    }

    private async Task DeletePost(int postId, string postTitle)
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm",
            $"投稿「{postTitle}」を削除してもよろしいですか?この操作は取り消せません。"))
        {
            return;
        }

        try
        {
            errorMessage = null;
            successMessage = null;

            var result = await PostService.DeletePostAsAdminAsync(postId);
            if (result)
            {
                successMessage = $"投稿「{postTitle}」を削除しました。";
                await LoadPosts();
            }
            else
            {
                errorMessage = "投稿の削除に失敗しました。";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"エラーが発生しました: {ex.Message}";
        }
    }
}
