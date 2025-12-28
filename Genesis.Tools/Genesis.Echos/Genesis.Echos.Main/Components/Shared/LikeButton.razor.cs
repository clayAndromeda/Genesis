using Genesis.Echos.Main.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Genesis.Echos.Main.Components.Shared;

public partial class LikeButton
{
    [Inject] private PostService PostService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter]
    public int PostId { get; set; }

    [Parameter]
    public int LikeCount { get; set; }

    [Parameter]
    public bool IsLiked { get; set; }

    [Parameter]
    public EventCallback OnLikeChanged { get; set; }

    private bool isDisabled = false;
    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            isDisabled = true;
        }
        else
        {
            IsLiked = await PostService.HasUserLikedAsync(PostId, currentUserId);
        }
    }

    private async Task HandleLikeToggle()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        try
        {
            var wasLiked = await PostService.ToggleLikeAsync(PostId, currentUserId);
            IsLiked = wasLiked;
            LikeCount = wasLiked ? LikeCount + 1 : LikeCount - 1;

            await OnLikeChanged.InvokeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling like: {ex.Message}");
        }
    }
}
