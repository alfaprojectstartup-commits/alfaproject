IF NOT EXISTS(SELECT 1 FROM Empresas WHERE Id = 1)
  INSERT INTO Empresas (Nome, Ativo) VALUES (N'Empresa Demo', 1);

-- Fases/páginas/campos de exemplo (EmpresaId=1)
IF NOT EXISTS(SELECT 1 FROM PhaseTemplates WHERE EmpresaId=1 AND Nome=N'Lead')
BEGIN
  INSERT INTO PhaseTemplates (EmpresaId, Nome, Ordem, Ativo) VALUES (1, N'Lead', 1, 1);
END

DECLARE @PhaseId INT = (SELECT TOP 1 Id FROM PhaseTemplates WHERE EmpresaId=1 AND Nome=N'Lead');

IF NOT EXISTS(SELECT 1 FROM PageTemplates WHERE EmpresaId=1 AND PhaseTemplateId=@PhaseId AND Titulo=N'Dados do Cliente')
BEGIN
  INSERT INTO PageTemplates (EmpresaId, PhaseTemplateId, Titulo, Ordem) VALUES (1, @PhaseId, N'Dados do Cliente', 1);
END

DECLARE @PageId INT = (SELECT TOP 1 Id FROM PageTemplates WHERE EmpresaId=1 AND PhaseTemplateId=@PhaseId AND Titulo=N'Dados do Cliente');

IF NOT EXISTS(SELECT 1 FROM FieldTemplates WHERE EmpresaId=1 AND PageTemplateId=@PageId AND NomeCampo='nome')
BEGIN
  INSERT INTO FieldTemplates (EmpresaId, PageTemplateId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Ativo)
  VALUES (1, @PageId, 'nome', N'Nome do cliente', 'Text', 1, 1, 1);
END

IF NOT EXISTS(SELECT 1 FROM FieldTemplates WHERE EmpresaId=1 AND PageTemplateId=@PageId AND NomeCampo='email')
BEGIN
  INSERT INTO FieldTemplates (EmpresaId, PageTemplateId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Ativo)
  VALUES (1, @PageId, 'email', N'E-mail', 'Text', 0, 2, 1);
END

IF NOT EXISTS(SELECT 1 FROM FieldTemplates WHERE EmpresaId=1 AND PageTemplateId=@PageId AND NomeCampo='status_lead')
BEGIN
  INSERT INTO FieldTemplates (EmpresaId, PageTemplateId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Ativo)
  VALUES (1, @PageId, 'status_lead', N'Status do lead', 'Select', 1, 3, 1);

  DECLARE @FieldId INT = SCOPE_IDENTITY();

  INSERT INTO FieldOptions (EmpresaId, FieldTemplateId, Texto, Valor, Ordem, Ativo)
  VALUES (1, @FieldId, N'Novo', NULL, 1, 1),
         (1, @FieldId, N'Qualificado', NULL, 2, 1),
         (1, @FieldId, N'Descartado', NULL, 3, 1);
END
