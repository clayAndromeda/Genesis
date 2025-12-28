using Genesis.Echos.Domain.Enums;

namespace Genesis.Echos.Domain.Entities;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Leader-only fields
    public bool IsRead { get; set; }
    public ImportanceLevel? Importance { get; set; }

    // Navigation properties
    public ApplicationUser Author { get; set; } = null!;
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
