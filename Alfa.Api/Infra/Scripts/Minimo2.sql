-- ===============================
-- Inserir Empresa
-- ===============================
IF NOT EXISTS(SELECT 1 FROM Empresas WHERE Id = 1)
BEGIN
    INSERT INTO Empresas (Nome, Ativo) VALUES (N'Empresa God', 1);
END

DECLARE @EmpresaId INT = (SELECT TOP 1 Id FROM Empresas WHERE Id = 1);

-- ===============================
-- Inserir Fun��es de Usu�rio
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

-- ===============================
-- Inserir Usu�rio Admin
-- ===============================
IF NOT EXISTS(SELECT 1 FROM Usuarios WHERE Email=N'admin@empresa.com')
BEGIN
    INSERT INTO Usuarios (Nome, Email, SenhaHash, FuncaoId, Ativo)
    VALUES (N'Admin', N'admin@empresa.com', N'hashficticio', @FuncaoAdminId, 1);
END

DECLARE @UsuarioAdminId INT = (SELECT TOP 1 Id FROM Usuarios WHERE Email=N'admin@empresa.com');

-- ===============================
-- Vincular Usu�rio � Empresa
-- ===============================
IF NOT EXISTS(SELECT 1 FROM UsuariosEmpresas WHERE UsuarioId=@UsuarioAdminId AND EmpresaId=@EmpresaId)
BEGIN
    INSERT INTO UsuariosEmpresas (UsuarioId, EmpresaId) VALUES (@UsuarioAdminId, @EmpresaId);
END

-- ===============================
-- Inserir FaseModelo
-- ===============================
IF NOT EXISTS(SELECT 1 FROM Fases WHERE EmpresaId=@EmpresaId AND Nome=N'Lead')
BEGIN
    INSERT INTO Fases (EmpresaId, Nome, Ordem, Ativo) VALUES (@EmpresaId, N'Lead', 1, 1);
END

DECLARE @FaseModeloId INT = (SELECT TOP 1 Id FROM Fases WHERE EmpresaId=@EmpresaId AND Nome=N'Lead');

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
    INSERT INTO CampoModelos (EmpresaId, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Ativo)
    VALUES (@EmpresaId, @PaginaModeloId, 'nome', N'Nome do cliente', 'Text', 1, 1, 1);
END

IF NOT EXISTS(SELECT 1 FROM CampoModelos WHERE EmpresaId=@EmpresaId AND PaginaModeloId=@PaginaModeloId AND NomeCampo='email')
BEGIN
    INSERT INTO CampoModelos (EmpresaId, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Ativo)
    VALUES (@EmpresaId, @PaginaModeloId, 'email', N'E-mail', 'Text', 0, 2, 1);
END

IF NOT EXISTS(SELECT 1 FROM CampoModelos WHERE EmpresaId=@EmpresaId AND PaginaModeloId=@PaginaModeloId AND NomeCampo='status_lead')
BEGIN
    INSERT INTO CampoModelos (EmpresaId, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Ativo)
    VALUES (@EmpresaId, @PaginaModeloId, 'status_lead', N'Status do lead', 'Select', 1, 3, 1);

    DECLARE @CampoStatusId INT = SCOPE_IDENTITY();

    INSERT INTO CampoConfiguracoes (EmpresaId, CampoModeloId, Texto, Valor, Ordem, Ativo)
    VALUES (@EmpresaId, @CampoStatusId, N'Novo', NULL, 1, 1),
           (@EmpresaId, @CampoStatusId, N'Qualificado', NULL, 2, 1),
           (@EmpresaId, @CampoStatusId, N'Descartado', NULL, 3, 1);
END

-- ===============================
-- Inserir ProcessoStatus e FaseStatus
-- ===============================
IF NOT EXISTS(SELECT 1 FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Andamento')
BEGIN
    INSERT INTO ProcessoStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Em Andamento');
END

IF NOT EXISTS(SELECT 1 FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Conclu�do')
BEGIN
    INSERT INTO ProcessoStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Conclu�do');
END

IF NOT EXISTS(SELECT 1 FROM FaseStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Andamento')
BEGIN
    INSERT INTO FaseStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Em Andamento');
END

IF NOT EXISTS(SELECT 1 FROM FaseStatus WHERE EmpresaId=@EmpresaId AND Status=N'Conclu�da')
BEGIN
    INSERT INTO FaseStatus (EmpresaId, Status) VALUES (@EmpresaId, N'Conclu�da');
END

DECLARE @ProcessoStatusId INT = (SELECT TOP 1 Id FROM ProcessoStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Andamento');
DECLARE @FaseStatusId INT = (SELECT TOP 1 Id FROM FaseStatus WHERE EmpresaId=@EmpresaId AND Status=N'Em Andamento');

-- ===============================
-- Inserir Processo
-- ===============================
IF NOT EXISTS(SELECT 1 FROM Processos WHERE EmpresaId=@EmpresaId AND Titulo=N'Processo de Teste')
BEGIN
    INSERT INTO Processos (EmpresaId, Titulo, Status) VALUES (@EmpresaId, N'Processo de Teste', @ProcessoStatusId);
END

DECLARE @ProcessoId INT = (SELECT TOP 1 Id FROM Processos WHERE EmpresaId=@EmpresaId AND Titulo=N'Processo de Teste');

-- ===============================
-- Inserir Fase
-- ===============================
IF NOT EXISTS(SELECT 1 FROM Fases WHERE EmpresaId=@EmpresaId AND ProcessoId=@ProcessoId AND FaseModeloId=@FaseModeloId)
BEGIN
    INSERT INTO Fases (EmpresaId, ProcessoId, FaseModeloId, NomeFase, Ordem, Status)
    VALUES (@EmpresaId, @ProcessoId, @FaseModeloId, N'Lead Inicial', 1, @FaseStatusId);
END