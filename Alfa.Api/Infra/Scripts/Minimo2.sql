-- ===============================
-- Inserir Empresa
-- ===============================
IF NOT EXISTS(SELECT 1 FROM Empresas WHERE Id = 1)
BEGIN
    INSERT INTO Empresas (Nome, Ativo) VALUES (N'Empresa God', 1);
END

DECLARE @EmpresaId INT = (SELECT TOP 1 Id FROM Empresas WHERE Id = 1);

-- ===============================
-- Inserir Funções de Usuário
-- ===============================
IF NOT EXISTS(SELECT 1 FROM UsuariosFuncoes WHERE EmpresaId=@EmpresaId AND Funcao=N'Administrador')
BEGIN
    INSERT INTO UsuariosFuncoes (Funcao, EmpresaId) VALUES (N'Administrador', @EmpresaId);
END

IF NOT EXISTS(SELECT 1 FROM UsuariosFuncoes WHERE EmpresaId=@EmpresaId AND Funcao=N'Vendedor')
BEGIN
    INSERT INTO UsuariosFuncoes (Funcao, EmpresaId) VALUES (N'Vendedor', @EmpresaId);
END

DECLARE @FuncaoAdminId INT = (SELECT TOP 1 Id FROM UsuariosFuncoes WHERE EmpresaId=@EmpresaId AND Funcao=N'Administrador');
DECLARE @FuncaoVendedorId INT = (SELECT TOP 1 Id FROM UsuariosFuncoes WHERE EmpresaId=@EmpresaId AND Funcao=N'Vendedor');

-- ===============================
-- Inserir Usuário Admin
-- ===============================
IF NOT EXISTS(SELECT 1 FROM Usuarios WHERE Email=N'admin@empresa.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, SenhaHash, FuncaoId, Ativo)
    VALUES (N'Admin', N'admin@empresa.com', N'hashficticio', @FuncaoAdminId, 1);
END

DECLARE @UsuarioAdminId INT = (SELECT TOP 1 Id FROM Usuarios WHERE Email=N'admin@empresa.com');

IF NOT EXISTS(SELECT 1 FROM Usuarios WHERE Email=N'marina@empresa.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, SenhaHash, FuncaoId, Ativo)
    VALUES (N'Marina Souza', N'marina@empresa.com', N'hashficticio', @FuncaoVendedorId, 1);
END

IF NOT EXISTS(SELECT 1 FROM Usuarios WHERE Email=N'joao@empresa.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, SenhaHash, FuncaoId, Ativo)
    VALUES (N'João Batista', N'joao@empresa.com', N'hashficticio', @FuncaoVendedorId, 1);
END

DECLARE @UsuarioMarinaId INT = (SELECT TOP 1 Id FROM Usuarios WHERE Email=N'marina@empresa.com');
DECLARE @UsuarioJoaoId INT = (SELECT TOP 1 Id FROM Usuarios WHERE Email=N'joao@empresa.com');

-- ===============================
-- Vincular Usuário à Empresa
-- ===============================
IF NOT EXISTS(SELECT 1 FROM UsuariosEmpresas WHERE UsuarioId=@UsuarioAdminId AND EmpresaId=@EmpresaId)
BEGIN
    INSERT INTO UsuariosEmpresas (UsuarioId, EmpresaId) VALUES (@UsuarioAdminId, @EmpresaId);
END

IF NOT EXISTS(SELECT 1 FROM UsuariosEmpresas WHERE UsuarioId=@UsuarioMarinaId AND EmpresaId=@EmpresaId)
BEGIN
    INSERT INTO UsuariosEmpresas (UsuarioId, EmpresaId) VALUES (@UsuarioMarinaId, @EmpresaId);
END

IF NOT EXISTS(SELECT 1 FROM UsuariosEmpresas WHERE UsuarioId=@UsuarioJoaoId AND EmpresaId=@EmpresaId)
BEGIN
    INSERT INTO UsuariosEmpresas (UsuarioId, EmpresaId) VALUES (@UsuarioJoaoId, @EmpresaId);
END

-- ===============================
-- Inserir Status
-- ===============================
IF NOT EXISTS(SELECT 1 FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Planejamento')
BEGIN
    INSERT INTO ProcessoStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Em Planejamento');
END
IF NOT EXISTS(SELECT 1 FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Andamento')
BEGIN
    INSERT INTO ProcessoStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Em Andamento');
END
IF NOT EXISTS(SELECT 1 FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Revisão')
BEGIN
    INSERT INTO ProcessoStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Em Revisão');
END
IF NOT EXISTS(SELECT 1 FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Concluído')
BEGIN
    INSERT INTO ProcessoStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Concluído');
END
IF NOT EXISTS(SELECT 1 FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Outros')
BEGIN
    INSERT INTO ProcessoStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Outros');
END

IF NOT EXISTS(SELECT 1 FROM FaseStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Andamento')
BEGIN
    INSERT INTO FaseStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Em Andamento');
END
IF NOT EXISTS(SELECT 1 FROM FaseStatus WHERE EmpresaId=@EmpresaId AND Status=N'Concluída')
BEGIN
    INSERT INTO FaseStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Concluída');
END

DECLARE @ProcessoStatusPadrao INT = (SELECT TOP 1 Id FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Andamento');
DECLARE @FaseStatusPadrao INT = (SELECT TOP 1 Id FROM FaseStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Andamento');

-- ===============================
-- Inserir FaseModelo
-- ===============================
IF NOT EXISTS(SELECT 1 FROM FaseModelos WHERE EmpresaId=@EmpresaId AND Titulo=N'Lead')
BEGIN
    INSERT INTO FaseModelos (EmpresaId, Titulo, Ordem, Ativo) VALUES (@EmpresaId, N'Lead', 1, 1);
END

DECLARE @FaseModeloId INT = (SELECT TOP 1 Id FROM FaseModelos WHERE EmpresaId=@EmpresaId AND Titulo=N'Lead');

-- ===============================
-- Inserir PaginaModelo
-- ===============================
IF NOT EXISTS(SELECT 1 FROM PaginaModelos WHERE EmpresaId=@EmpresaId AND FaseModeloId=@FaseModeloId AND Titulo=N'Dados do Cliente')
BEGIN
    INSERT INTO PaginaModelos (EmpresaId, FaseModeloId, Titulo, Ordem) VALUES (@EmpresaId, @FaseModeloId, N'Dados do Cliente', 1);
END

DECLARE @PaginaModeloId INT = (SELECT TOP 1 Id FROM PaginaModelos WHERE EmpresaId=@EmpresaId AND FaseModeloId=@FaseModeloId AND Titulo=N'Dados do Cliente');

-- ===============================
-- Inserir CampoModelos
-- ===============================
IF NOT EXISTS(SELECT 1 FROM CampoModelos WHERE EmpresaId=@EmpresaId AND PaginaModeloId=@PaginaModeloId AND NomeCampo='nome')
BEGIN
    INSERT INTO CampoModelos (EmpresaId, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Placeholder, Mascara, Ajuda, Ordem, Ativo)
    VALUES (@EmpresaId, @PaginaModeloId, 'nome', N'Nome do cliente', 'Text', 1, N'Digite o nome', NULL, NULL, 1, 1);
END

IF NOT EXISTS(SELECT 1 FROM CampoModelos WHERE EmpresaId=@EmpresaId AND PaginaModeloId=@PaginaModeloId AND NomeCampo='email')
BEGIN
    INSERT INTO CampoModelos (EmpresaId, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Placeholder, Mascara, Ajuda, Ordem, Ativo)
    VALUES (@EmpresaId, @PaginaModeloId, 'email', N'E-mail', 'Text', 0, N'nome@empresa.com', NULL, NULL, 2, 1);
END

IF NOT EXISTS(SELECT 1 FROM CampoModelos WHERE EmpresaId=@EmpresaId AND PaginaModeloId=@PaginaModeloId AND NomeCampo='status_lead')
BEGIN
    INSERT INTO CampoModelos (EmpresaId, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Placeholder, Mascara, Ajuda, Ordem, Ativo)
    VALUES (@EmpresaId, @PaginaModeloId, 'status_lead', N'Status do lead', 'Select', 1, NULL, NULL, N'Selecione o status', 3, 1);

    DECLARE @CampoStatusId INT = SCOPE_IDENTITY();

    INSERT INTO CampoConfiguracoes (EmpresaId, CampoModeloId, Texto, Valor, Ordem, Ativo)
    VALUES (@EmpresaId, @CampoStatusId, N'Novo', NULL, 1, 1),
           (@EmpresaId, @CampoStatusId, N'Qualificado', NULL, 2, 1),
           (@EmpresaId, @CampoStatusId, N'Descartado', NULL, 3, 1);
END

-- ===============================
-- Inserir Processo de Exemplo
-- ===============================
IF NOT EXISTS(SELECT 1 FROM Processos WHERE EmpresaId=@EmpresaId AND Titulo=N'Processo de Teste')
BEGIN
    INSERT INTO Processos (EmpresaId, Titulo, StatusId)
    VALUES (@EmpresaId, N'Processo de Teste', @ProcessoStatusPadrao);
END

DECLARE @ProcessoId INT = (SELECT TOP 1 Id FROM Processos WHERE EmpresaId=@EmpresaId AND Titulo=N'Processo de Teste');

IF NOT EXISTS(SELECT 1 FROM ProcessoHistoricos WHERE EmpresaId=@EmpresaId AND ProcessoId=@ProcessoId)
BEGIN
    INSERT INTO ProcessoHistoricos (EmpresaId, ProcessoId, UsuarioId, UsuarioNome, Descricao, CriadoEm)
    VALUES
        (@EmpresaId, @ProcessoId, @UsuarioAdminId, N'Admin', N'Criou o processo', DATEADD(DAY, -5, SYSDATETIME())),
        (@EmpresaId, @ProcessoId, @UsuarioMarinaId, N'Marina Souza', N'Atualizou o status para "Em andamento"', DATEADD(DAY, -2, SYSDATETIME())),
        (@EmpresaId, @ProcessoId, @UsuarioJoaoId, N'João Batista', N'Realizou ajustes finais', DATEADD(DAY, -1, SYSDATETIME()));
END

-- ===============================
-- Garantir Fase Instanciada
-- ===============================
IF NOT EXISTS(SELECT 1 FROM FaseInstancias WHERE EmpresaId=@EmpresaId AND ProcessoId=@ProcessoId AND FaseModeloId=@FaseModeloId)
BEGIN
    INSERT INTO FaseInstancias (EmpresaId, ProcessoId, FaseModeloId, Titulo, Ordem, StatusId)
    VALUES (@EmpresaId, @ProcessoId, @FaseModeloId, N'Lead Inicial', 1, @FaseStatusPadrao);
END

DECLARE @FaseInstanciaId INT = (SELECT TOP 1 Id FROM FaseInstancias WHERE EmpresaId=@EmpresaId AND ProcessoId=@ProcessoId AND FaseModeloId=@FaseModeloId);

-- ===============================
-- Garantir Pagina Instanciada
-- ===============================
IF NOT EXISTS(SELECT 1 FROM PaginaInstancias WHERE EmpresaId=@EmpresaId AND FaseInstanciaId=@FaseInstanciaId AND PaginaModeloId=@PaginaModeloId)
BEGIN
    INSERT INTO PaginaInstancias (EmpresaId, FaseInstanciaId, PaginaModeloId, Titulo, Ordem)
    VALUES (@EmpresaId, @FaseInstanciaId, @PaginaModeloId, N'Dados do Cliente', 1);
END

DECLARE @PaginaInstanciaId INT = (SELECT TOP 1 Id FROM PaginaInstancias WHERE EmpresaId=@EmpresaId AND FaseInstanciaId=@FaseInstanciaId AND PaginaModeloId=@PaginaModeloId);

-- ===============================
-- Garantir Campos Instanciados
-- ===============================
INSERT INTO CampoInstancias (EmpresaId, PaginaInstanciaId, CampoModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Placeholder, Mascara, Ajuda)
SELECT ci.EmpresaId, @PaginaInstanciaId, cm.Id, cm.NomeCampo, cm.Rotulo, cm.Tipo, cm.Obrigatorio, cm.Ordem, cm.Placeholder, cm.Mascara, cm.Ajuda
FROM CampoModelos cm
CROSS APPLY (SELECT @EmpresaId AS EmpresaId) ci
WHERE cm.EmpresaId=@EmpresaId
  AND cm.PaginaModeloId=@PaginaModeloId
  AND NOT EXISTS(
        SELECT 1 FROM CampoInstancias c
        WHERE c.EmpresaId=@EmpresaId
          AND c.PaginaInstanciaId=@PaginaInstanciaId
          AND c.CampoModeloId=cm.Id
    );
