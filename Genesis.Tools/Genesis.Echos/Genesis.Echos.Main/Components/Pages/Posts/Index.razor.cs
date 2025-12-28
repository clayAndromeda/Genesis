using Genesis.Echos.Domain.Entities;
using Genesis.Echos.Main.Services;
using Microsoft.AspNetCore.Components;

namespace Genesis.Echos.Main.Components.Pages.Posts;

public partial class Index
{
    [Inject] private PostService PostService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<Post>? posts;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            posts = await PostService.GetAllPostsAsync();
        }
        catch (Exception ex)
        {
            // TODO: エラーハンドリング
            Console.WriteLine($"Error loading posts: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }
}
