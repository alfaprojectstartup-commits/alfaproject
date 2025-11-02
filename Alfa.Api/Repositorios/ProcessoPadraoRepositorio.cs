using System.Linq;
using Alfa.Api.Dtos;
using Alfa.Api.Infra.Interfaces;
using Alfa.Api.Repositorios.Interfaces;
using Dapper;

namespace Alfa.Api.Repositorios
{
    public class ProcessoPadraoRepositorio : IProcessoPadraoRepositorio
    {
        private readonly IConexaoSql _db;

        public ProcessoPadraoRepositorio(IConexaoSql db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ProcessoPadraoModeloDto>> ListarAsync(int empresaId)
        {
            using var conn = await _db.AbrirConexaoAsync();

            const string sql = @"
                SELECT
                    ppm.Id,
                    ppm.Titulo,
                    ppm.Descricao,
                    fmf.FaseModeloId,
                    fmf.Ordem
                FROM ProcessoPadraoModelos ppm
                LEFT JOIN ProcessoPadraoModeloFases fmf
                       ON fmf.ProcessoPadraoModeloId = ppm.Id
                WHERE ppm.EmpresaId = @EmpresaId
                ORDER BY ppm.Titulo, fmf.Ordem, fmf.FaseModeloId;";

            var rows = await conn.QueryAsync<ProcessoPadraoModeloRow>(sql, new { EmpresaId = empresaId });

            return rows
                .GroupBy(r => new { r.Id, r.Titulo, r.Descricao })
                .Select(group => new ProcessoPadraoModeloDto(
                    group.Key.Id,
                    group.Key.Titulo,
                    group.Key.Descricao,
                    group
                        .Where(r => r.FaseModeloId.HasValue)
                        .OrderBy(r => r.Ordem ?? int.MaxValue)
                        .ThenBy(r => r.FaseModeloId)
                        .Select(r => r.FaseModeloId!.Value)
                        .ToArray()))
                .OrderBy(dto => dto.Titulo)
                .ToList();
        }

        public async Task<int> CriarAsync(int empresaId, ProcessoPadraoModeloInputDto dto)
        {
            using var conn = await _db.AbrirConexaoAsync();
            using var tx = conn.BeginTransaction();

            var novoId = await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO ProcessoPadraoModelos (EmpresaId, Titulo, Descricao)
                  VALUES (@EmpresaId, @Titulo, @Descricao);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { EmpresaId = empresaId, dto.Titulo, dto.Descricao }, tx);

            if (dto.FaseModeloIds?.Any() == true)
            {
                var ordem = 1;
                foreach (var faseId in dto.FaseModeloIds.Distinct())
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO ProcessoPadraoModeloFases (ProcessoPadraoModeloId, FaseModeloId, Ordem)
                          VALUES (@PadraoId, @FaseModeloId, @Ordem);",
                        new
                        {
                            PadraoId = novoId,
                            FaseModeloId = faseId,
                            Ordem = ordem++
                        }, tx);
                }
            }

            tx.Commit();
            return novoId;
        }

        private sealed record ProcessoPadraoModeloRow
        {
            public int Id { get; init; }
            public string Titulo { get; init; } = string.Empty;
            public string? Descricao { get; init; }
            public int? FaseModeloId { get; init; }
            public int? Ordem { get; init; }
        }
    }
}
