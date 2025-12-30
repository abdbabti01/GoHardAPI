using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.DTOs
{
    public record LoginRequest(
        [Required][EmailAddress] string Email,
        [Required] string Password
    );

    public record SignupRequest(
        [Required][MaxLength(100)] string Name,
        [Required][EmailAddress] string Email,
        [Required][MinLength(6)] string Password
    );

    public record AuthResponse(
        string Token,
        int UserId,
        string Name,
        string Email
    );
}
