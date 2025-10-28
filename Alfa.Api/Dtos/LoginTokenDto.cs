namespace Alfa.Api.Dtos
{
    public class LoginTokenDto
    {
        public required string Email { get; set; }
        public required int FuncaoId { get; set; }
        public required string Token { get; set; }
    }
}
