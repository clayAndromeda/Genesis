namespace Genesis.Echos.Domain.Entities;

public class PostTag
{
    public int PostId { get; set; }
    public int TagId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Post Post { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
