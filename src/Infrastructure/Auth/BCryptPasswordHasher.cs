using SupportDesk.Application.Auth;

namespace SupportDesk.Infrastructure.Auth;

public class BCryptPasswordHasher : IPasswordHasher
{
    // Work factor 12 is a reasonable default in 2026 — high enough to resist
    // brute force, low enough not to noticeably slow down login. Bump it over
    // time as hardware gets faster; BCrypt.Net handles verifying old hashes
    // hashed at a lower work factor without any migration needed.
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: WorkFactor);

    public bool Verify(string plainTextPassword, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainTextPassword, hash);
}