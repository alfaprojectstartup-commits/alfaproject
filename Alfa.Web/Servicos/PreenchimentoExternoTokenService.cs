using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Alfa.Web.Servicos;

public class PreenchimentoExternoTokenService
{
    private const string ProtectorPurpose = "Alfa.Web.Processos.PreenchimentoExterno";

    private readonly ITimeLimitedDataProtector _protector;

    public PreenchimentoExternoTokenService(IDataProtectionProvider provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        _protector = provider
            .CreateProtector(ProtectorPurpose)
            .ToTimeLimitedDataProtector();
    }

    public string GerarToken(int processoId, int faseInstanciaId, int paginaInstanciaId, TimeSpan? validade)
    {
        var payload = new TokenPayload
        {
            ProcessoId = processoId,
            FaseInstanciaId = faseInstanciaId,
            PaginaInstanciaId = paginaInstanciaId,
            EmitidoEm = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);

        return validade.HasValue
            ? _protector.Protect(json, validade.Value)
            : _protector.Protect(json);
    }

    public bool TryValidarToken(string? token, out TokenPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var json = _protector.Unprotect(token);
            payload = JsonSerializer.Deserialize<TokenPayload>(json);
            return payload is not null
                && payload.ProcessoId > 0
                && payload.FaseInstanciaId > 0
                && payload.PaginaInstanciaId > 0;
        }
        catch
        {
            payload = null;
            return false;
        }
    }

    public record TokenPayload
    {
        public int ProcessoId { get; init; }
        public int FaseInstanciaId { get; init; }
        public int PaginaInstanciaId { get; init; }
        public DateTimeOffset EmitidoEm { get; init; }
    }
}
