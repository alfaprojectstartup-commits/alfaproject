using System;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Alfa.Web.Servicos;

public interface IUrlProtector
{
    string Encode(string purpose, int id);
    bool TryDecode(string purpose, string? token, out int id);
}

public class UrlProtector : IUrlProtector
{
    private readonly IDataProtectionProvider _provider;

    public UrlProtector(IDataProtectionProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public string Encode(string purpose, int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        var protector = CreateProtector(purpose);
        return protector.Protect(id.ToString(CultureInfo.InvariantCulture));
    }

    public bool TryDecode(string purpose, string? token, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var protector = CreateProtector(purpose);
            var unprotected = protector.Unprotect(token);
            return int.TryParse(unprotected, NumberStyles.Integer, CultureInfo.InvariantCulture, out id) && id > 0;
        }
        catch (CryptographicException)
        {
            id = 0;
            return false;
        }
        catch (FormatException)
        {
            id = 0;
            return false;
        }
    }

    private IDataProtector CreateProtector(string purpose)
    {
        var normalizedPurpose = string.IsNullOrWhiteSpace(purpose)
            ? nameof(UrlProtector)
            : purpose.Trim();

        return _provider.CreateProtector($"Alfa.Web.UrlProtector:{normalizedPurpose}");
    }
}
