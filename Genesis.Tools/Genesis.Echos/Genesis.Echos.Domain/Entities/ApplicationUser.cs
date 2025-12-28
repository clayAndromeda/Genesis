using Genesis.Echos.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Genesis.Echos.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
