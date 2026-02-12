public interface ICurrentUserService
{
    string? UserId { get; }
    bool IsAdmin { get; }
}
