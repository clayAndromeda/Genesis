namespace Genesis.Echos.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6c757d"; // Bootstrap secondary color
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}
