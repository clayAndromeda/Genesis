using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Main.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Genesis.Echos.Main.Components.Pages.Posts;

public partial class Edit
{
    [Inject] private PostService PostService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    private Post? post;
    private PostModel model = new();
    private bool isLoading = true;
    private bool isSubmitting = false;
    private bool isAuthor = false;
    private string? errorMessage;
    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            post = await PostService.GetPostByIdAsync(Id);

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (post != null && currentUserId == post.AuthorId)
            {
                isAuthor = true;
                model.Title = post.Title;
                model.Content = post.Content;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"投稿の読み込み中にエラーが発生しました: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleValidSubmit()
    {
        if (post == null || string.IsNullOrEmpty(currentUserId)) return;

        try
        {
            isSubmitting = true;
            errorMessage = null;

            post.Title = model.Title;
            post.Content = model.Content;

            var success = await PostService.UpdatePostAsync(post, currentUserId);
            if (success)
            {
                Navigation.NavigateTo($"/posts/{Id}");
            }
            else
            {
                errorMessage = "投稿の更新に失敗しました。";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"投稿の更新中にエラーが発生しました: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo($"/posts/{Id}");
    }

    public class PostModel
    {
        [Required(ErrorMessage = "タイトルは必須です")]
        [StringLength(200, ErrorMessage = "タイトルは200文字以内で入力してください")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "本文は必須です")]
        [StringLength(5000, ErrorMessage = "本文は5000文字以内で入力してください")]
        public string Content { get; set; } = string.Empty;
    }
}
