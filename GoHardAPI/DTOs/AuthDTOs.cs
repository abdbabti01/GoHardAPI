using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.DTOs
{
    public record LoginRequest(
        [Required][EmailAddress] string Email,
        [Required] string Password
    );

    public record SignupRequest(
        [Required][MaxLength(100)] string Name,
        [Required]
        [MaxLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        string Username,
        [Required][EmailAddress] string Email,
        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        string Password
    );

    public record AuthResponse(
        string Token,
        int UserId,
        string Name,
        string Username,
        string Email
    );
}
