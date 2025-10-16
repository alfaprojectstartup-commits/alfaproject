-- Empresas
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='Empresas')
BEGIN
  CREATE TABLE Empresas(
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    Nome      NVARCHAR(200) NOT NULL,
    Ativo     BIT NOT NULL CONSTRAINT DF_Emp_Ativo DEFAULT(1)
  );
END

-- Usuarios (simples, sem Identity)
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='Usuarios')
BEGIN
  CREATE TABLE Usuarios(
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Email      NVARCHAR(200) NOT NULL UNIQUE,
    Nome       NVARCHAR(150) NOT NULL,
    SenhaHash  NVARCHAR(200) NULL,
    Ativo      BIT NOT NULL CONSTRAINT DF_User_Ativo DEFAULT(1)
  );
END

-- Vinculo Usuario-Empresa
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='UsuariosEmpresas')
BEGIN
  CREATE TABLE UsuariosEmpresas(
    UserId    INT NOT NULL,
    EmpresaId INT NOT NULL,
    CONSTRAINT PK_UsuariosEmpresas PRIMARY KEY(UserId, EmpresaId),
    CONSTRAINT FK_UE_User FOREIGN KEY(UserId) REFERENCES Usuarios(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UE_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id) ON DELETE CASCADE
  );
END

-- PhaseTemplates
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='PhaseTemplates')
BEGIN
  CREATE TABLE PhaseTemplates(
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId  INT NOT NULL,
    Nome       NVARCHAR(100) NOT NULL,
    Ordem      INT NOT NULL,
    Ativo      BIT NOT NULL CONSTRAINT DF_PhaseT_Ativo DEFAULT(1),
    CONSTRAINT FK_PhaseT_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
  );
  CREATE INDEX IX_PhaseTemplates_Emp_Ord ON PhaseTemplates(EmpresaId, Ordem);
END

-- PageTemplates
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='PageTemplates')
BEGIN
  CREATE TABLE PageTemplates(
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId      INT NOT NULL,
    PhaseTemplateId INT NOT NULL,
    Titulo         NVARCHAR(150) NOT NULL,
    Ordem          INT NOT NULL,
    CONSTRAINT FK_PageT_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_PageT_PhaseT FOREIGN KEY(PhaseTemplateId) REFERENCES PhaseTemplates(Id)
  );
  CREATE INDEX IX_PageTemplates_Emp_Phase_Ord ON PageTemplates(EmpresaId, PhaseTemplateId, Ordem);
END

-- FieldTemplates
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FieldTemplates')
BEGIN
  CREATE TABLE FieldTemplates(
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId      INT NOT NULL,
    PageTemplateId INT NOT NULL,
    NomeCampo      NVARCHAR(120) NOT NULL,
    Rotulo         NVARCHAR(150) NOT NULL,
    Tipo           NVARCHAR(30) NOT NULL, -- Text, TextArea, Number, Date, Checkbox, Select, File, Image...
    Obrigatorio    BIT NOT NULL,
    Placeholder    NVARCHAR(150) NULL,
    MaxLength      INT NULL,
    Min            DECIMAL(18,4) NULL,
    Max            DECIMAL(18,4) NULL,
    Decimals       INT NULL,
    Mask           NVARCHAR(50) NULL,
    Ordem          INT NOT NULL,
    Ativo          BIT NOT NULL CONSTRAINT DF_FieldT_Ativo DEFAULT(1),
    CONSTRAINT FK_FieldT_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_FieldT_PageT FOREIGN KEY(PageTemplateId) REFERENCES PageTemplates(Id)
  );
  CREATE INDEX IX_FieldTemplates_Emp_Page_Ord ON FieldTemplates(EmpresaId, PageTemplateId, Ordem);
END

-- FieldOptions (para Select/Checkbox list)
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FieldOptions')
BEGIN
  CREATE TABLE FieldOptions(
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId      INT NOT NULL,
    FieldTemplateId INT NOT NULL,
    Texto          NVARCHAR(150) NOT NULL,
    Valor          NVARCHAR(150) NULL,
    Ordem          INT NOT NULL,
    Ativo          BIT NOT NULL CONSTRAINT DF_FieldOpt_Ativo DEFAULT(1),
    CONSTRAINT FK_FieldOpt_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_FieldOpt_FieldT FOREIGN KEY(FieldTemplateId) REFERENCES FieldTemplates(Id)
  );
  CREATE INDEX IX_FieldOptions_Emp_Field_Ord ON FieldOptions(EmpresaId, FieldTemplateId, Ordem);
END

-- ProcessInstances
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='ProcessInstances')
BEGIN
  CREATE TABLE ProcessInstances(
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId    INT NOT NULL,
    Titulo       NVARCHAR(200) NOT NULL,
    Status       NVARCHAR(40) NOT NULL CONSTRAINT DF_Process_Status DEFAULT('EmAndamento'),
    ProgressoPct INT NOT NULL CONSTRAINT DF_Process_Prog DEFAULT(0),
    CriadoEm     DATETIME2 NOT NULL CONSTRAINT DF_Process_Criado DEFAULT(SYSDATETIME()),
    CONSTRAINT FK_Proc_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
  );
  CREATE INDEX IX_ProcessInstances_Emp ON ProcessInstances(EmpresaId, Id DESC);
END

-- PhaseInstances
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='PhaseInstances')
BEGIN
  CREATE TABLE PhaseInstances(
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId        INT NOT NULL,
    ProcessInstanceId INT NOT NULL,
    PhaseTemplateId  INT NOT NULL,
    NomeFase         NVARCHAR(100) NOT NULL,
    Ordem            INT NOT NULL,
    Status           NVARCHAR(40) NOT NULL CONSTRAINT DF_Phase_Status DEFAULT('EmAndamento'),
    ProgressoPct     INT NOT NULL CONSTRAINT DF_Phase_Prog DEFAULT(0),
    CONSTRAINT FK_PhaseI_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_PhaseI_Proc FOREIGN KEY(ProcessInstanceId) REFERENCES ProcessInstances(Id),
    CONSTRAINT FK_PhaseI_PhaseT FOREIGN KEY(PhaseTemplateId) REFERENCES PhaseTemplates(Id)
  );
  CREATE INDEX IX_PhaseInstances_Emp_Proc_Ord ON PhaseInstances(EmpresaId, ProcessInstanceId, Ordem);
END

-- PageResponses
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='PageResponses')
BEGIN
  CREATE TABLE PageResponses(
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId       INT NOT NULL,
    PhaseInstanceId INT NOT NULL,
    PageTemplateId  INT NOT NULL,
    CriadoEm        DATETIME2 NOT NULL CONSTRAINT DF_PageResp_Criado DEFAULT(SYSDATETIME()),
    CONSTRAINT FK_PageResp_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_PageResp_PhaseI FOREIGN KEY(PhaseInstanceId) REFERENCES PhaseInstances(Id),
    CONSTRAINT FK_PageResp_PageT FOREIGN KEY(PageTemplateId) REFERENCES PageTemplates(Id)
  );
  CREATE INDEX IX_PageResponses_Emp_Phase_Page ON PageResponses(EmpresaId, PhaseInstanceId, PageTemplateId);
END

-- FieldResponses
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FieldResponses')
BEGIN
  CREATE TABLE FieldResponses(
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId      INT NOT NULL,
    PageResponseId INT NOT NULL,
    FieldTemplateId INT NOT NULL,
    ValorTexto     NVARCHAR(MAX) NULL,
    ValorNumero    DECIMAL(18,4) NULL,
    ValorData      DATETIME2 NULL,
    ValorBool      BIT NULL,
    CONSTRAINT FK_FieldResp_Emp FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_FieldResp_PageR FOREIGN KEY(PageResponseId) REFERENCES PageResponses(Id),
    CONSTRAINT FK_FieldResp_FieldT FOREIGN KEY(FieldTemplateId) REFERENCES FieldTemplates(Id)
  );
  CREATE INDEX IX_FieldResponses_Emp_PageR_Field ON FieldResponses(EmpresaId, PageResponseId, FieldTemplateId);
END
