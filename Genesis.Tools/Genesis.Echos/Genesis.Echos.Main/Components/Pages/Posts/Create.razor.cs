using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Main.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Genesis.Echos.Main.Components.Pages.Posts;

public partial class Create
{
    [Inject] private PostService PostService { get; set; } = default!;
    [Inject] private TagService TagService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private PostModel model = new();
    private bool isSubmitting = false;
    private string? errorMessage;
    private List<Tag> availableTags = new();
    private List<int> selectedTagIds = new();

    protected override async Task OnInitializedAsync()
    {
        availableTags = await TagService.GetAllTagsAsync();
    }

    private async Task HandleValidSubmit()
    {
        try
        {
            isSubmitting = true;
            errorMessage = null;

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                errorMessage = "ユーザー情報を取得できませんでした。";
                return;
            }

            var post = new Post
            {
                Title = model.Title,
                Content = model.Content,
                AuthorId = userId
            };

            await PostService.CreatePostAsync(post, selectedTagIds);
            Navigation.NavigateTo("/posts");
        }
        catch (Exception ex)
        {
            errorMessage = $"投稿の作成中にエラーが発生しました: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/posts");
    }

    private void ToggleTag(int tagId)
    {
        if (selectedTagIds.Contains(tagId))
        {
            selectedTagIds.Remove(tagId);
        }
        else
        {
            selectedTagIds.Add(tagId);
        }
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
