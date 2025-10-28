using Microsoft.AspNetCore.Identity;

namespace Alfa.Api.Utils
{
    public static class SenhaHash
    {
        public static string HashSenha(string email, string senha)
        {
            var hasher = new PasswordHasher<string>();
            return hasher.HashPassword(email, senha);
        }

        public static bool VerificarSenhaHash(string email, string senhaHash, string senha)
        {
            var hasher = new PasswordHasher<string>();
            return hasher.VerifyHashedPassword(email, senhaHash, senha) == PasswordVerificationResult.Success;
        }
    }
}
