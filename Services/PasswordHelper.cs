using System.Security.Cryptography;
using System.Text;

namespace HRMS.Services;

public static class PasswordHelper
{
  private const int SaltSize = 16;
  private const int HashSize = 32;
  private const int Iterations = 100_000;

  public static string HashPassword(string password)
  {
    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var hash = Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);

    return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
  }

  public static bool VerifyPassword(string password, string storedHash)
  {
    if (string.IsNullOrWhiteSpace(storedHash)) return false;

    var parts = storedHash.Split('.', 2);
    if (parts.Length != 2) return false;

    byte[] salt, expected;
    try
    {
      salt     = Convert.FromBase64String(parts[0]);
      expected = Convert.FromBase64String(parts[1]);
    }
    catch
    {
      return false;
    }

    var actual = Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);

    return CryptographicOperations.FixedTimeEquals(actual, expected);
  }
}
