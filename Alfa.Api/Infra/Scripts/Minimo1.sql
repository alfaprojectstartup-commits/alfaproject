-- Empresas
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='Empresas')
BEGIN
  CREATE TABLE Empresas(
    Id     INT IDENTITY(1,1) PRIMARY KEY,
    Nome   NVARCHAR(250) NOT NULL,
    Ativo  BIT NOT NULL CONSTRAINT DF_Empresas_Ativo DEFAULT(1)
  );
END

-- Funções dos Usuários
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name = 'UsuariosFuncoes')
BEGIN
  CREATE TABLE UsuariosFuncoes (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Funcao     NVARCHAR(50) NOT NULL,
	EmpresaId  INT NOT NULL,
	CONSTRAINT FK_UsuariosFuncoes_Empresas_Id FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
  );
  CREATE INDEX Index_UsuariosFuncoes_Empresas ON UsuariosFuncoes(EmpresaId, Id);
END

-- Usuarios
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name = 'Usuarios')
BEGIN
  CREATE TABLE Usuarios (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Nome       NVARCHAR(250) NOT NULL,
    Email      NVARCHAR(250) NOT NULL,
    SenhaHash  NVARCHAR(200) NOT NULL,
    FuncaoId   INT NOT NULL,
    Ativo      BIT NOT NULL CONSTRAINT DF_Usuarios_Ativo DEFAULT(1),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT FK_Usuarios_UsuariosFuncoes_Id FOREIGN KEY (FuncaoId) REFERENCES UsuariosFuncoes(Id)
  );
END

-- Vínculo Usuário e Empresa
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='UsuariosEmpresas')
BEGIN
  CREATE TABLE UsuariosEmpresas(
    UsuarioId  INT NOT NULL,
    EmpresaId  INT NOT NULL,
    CONSTRAINT PK_UsuariosEmpresas PRIMARY KEY(UsuarioId, EmpresaId),
    CONSTRAINT FK_UsuariosEmpresas_Usuarios_Id FOREIGN KEY(UsuarioId) REFERENCES Usuarios(Id),
    CONSTRAINT FK_UsuariosEmpresas_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
  );
END

-- FaseModelos
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FaseModelos')
BEGIN
  CREATE TABLE FaseModelos(
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId  INT NOT NULL,
    Nome       NVARCHAR(100) NOT NULL,
    Ordem      INT NOT NULL,
    Ativo      BIT NOT NULL CONSTRAINT DF_FaseModelos_Ativo DEFAULT(1),
    CONSTRAINT FK_FaseModelos_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
  );
  CREATE INDEX Index_FaseModelos_Empresas_Ordem ON FaseModelos(EmpresaId, Ordem);
END

-- PaginaModelos
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='PaginaModelos')
BEGIN
  CREATE TABLE PaginaModelos(
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId     INT NOT NULL,
    FaseModeloId  INT NOT NULL,
    Titulo        NVARCHAR(150) NOT NULL,
    Ordem         INT NOT NULL,
    CONSTRAINT FK_PaginaModelos_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_PaginaModelos_FaseModelos_Id FOREIGN KEY(FaseModeloId) REFERENCES FaseModelos(Id)
  );
  CREATE INDEX Index_PaginaModelos_FaseModelos_Empresas_Ordem ON PaginaModelos(EmpresaId, FaseModeloId, Ordem);
END

-- CampoModelos
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='CampoModelos')
BEGIN
  CREATE TABLE CampoModelos(
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId       INT NOT NULL,
    PaginaModeloId  INT NOT NULL,
    NomeCampo       NVARCHAR(120) NOT NULL,
    Rotulo          NVARCHAR(150) NOT NULL,
    Tipo            NVARCHAR(30) NOT NULL, -- Text, TextArea, Number, Date, Checkbox, Select, File, Image...
    Obrigatorio     BIT NOT NULL,
    Placeholder     NVARCHAR(150) NULL,
    TamanhoMaximo   INT NULL,
    Min             DECIMAL(18,4) NULL,
    Max             DECIMAL(18,4) NULL,
    Decimals        INT NULL,
    Mask            NVARCHAR(50) NULL,
    Ordem           INT NOT NULL,
    Ativo           BIT NOT NULL CONSTRAINT DF_CampoModelos_Ativo DEFAULT(1),
    CONSTRAINT FK_CampoModelos_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_CampoModelos_PaginaModelos_Id FOREIGN KEY(PaginaModeloId) REFERENCES PaginaModelos(Id)
  );
  CREATE INDEX Index_CampoModelos_PaginaModelos_Empresas_Ordem ON CampoModelos(EmpresaId, PaginaModeloId, Ordem);
END

-- CampoConfiguracoes (para Select/Checkbox list)
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='CampoConfiguracoes')
BEGIN
  CREATE TABLE CampoConfiguracoes(
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId      INT NOT NULL,
    CampoModeloId  INT NOT NULL,
    Texto          NVARCHAR(150) NOT NULL,
    Valor          NVARCHAR(150) NULL,
    Ordem          INT NOT NULL,
    Ativo          BIT NOT NULL CONSTRAINT DF_CampoConfiguracoes_Ativo DEFAULT(1),
    CONSTRAINT FK_CampoConfiguracoes_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_CampoConfiguracoes_CampoModelos_Id FOREIGN KEY(CampoModeloId) REFERENCES CampoModelos(Id)
  );
  CREATE INDEX Index_CampoConfiguracoes_CampoModelos_Empresas_Ordem ON CampoConfiguracoes(EmpresaId, CampoModeloId, Ordem);
END

-- ProcessoStatus
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='ProcessoStatus')
BEGIN
  CREATE TABLE ProcessoStatus(
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Status     NVARCHAR(100) NOT NULL,
	EmpresaId  INT NOT NULL,
    CONSTRAINT FK_ProcessoStatus_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
  );
  CREATE INDEX Index_ProcessoStatus_Empresas ON ProcessoStatus(EmpresaId, Id);
END

-- Processos
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='Processos')
BEGIN
  CREATE TABLE Processos(
    Id            		 INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId     		 INT NOT NULL,
    Titulo        		 NVARCHAR(200) NOT NULL,
    Status               INT NOT NULL,
    PorcentagemProgresso  INT NOT NULL CONSTRAINT DF_Processos_PorcentagemProgresso DEFAULT(0),
    CriadoEm      		 DATETIME2 NOT NULL CONSTRAINT DF_Processos_CriadoEm DEFAULT(SYSDATETIME()),
    CONSTRAINT FK_Processos_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
	CONSTRAINT FK_Processos_ProcessoStatus_Id FOREIGN KEY(Status) REFERENCES ProcessoStatus(Id)
  );
  CREATE INDEX Index_Processos_Empresas ON Processos(EmpresaId, Id DESC);
END

-- FaseStatus
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FaseStatus')
BEGIN
  CREATE TABLE FaseStatus(
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Status     NVARCHAR(100) NOT NULL,
	EmpresaId  INT NOT NULL,
    CONSTRAINT FK_FaseStatus_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
  );
  CREATE INDEX Index_FaseStatus_Empresas ON FaseStatus(EmpresaId, Id);
END

-- Fases
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='Fases')
BEGIN
  CREATE TABLE Fases(
    Id            		  INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId     		  INT NOT NULL,
    ProcessoId    		  INT NOT NULL,
    FaseModeloId  		  INT NOT NULL,
    NomeFase      		  NVARCHAR(100) NOT NULL,
    Ordem         		  INT NOT NULL,
    Status        		  INT NOT NULL,
    PorcentagemProgresso  INT NOT NULL CONSTRAINT DF_Fases_PorcentagemProgresso DEFAULT(0),
    CONSTRAINT FK_Fases_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_Fases_Processos_Id FOREIGN KEY(ProcessoId) REFERENCES Processos(Id),
    CONSTRAINT FK_Fases_FaseModelos_Id FOREIGN KEY(FaseModeloId) REFERENCES FaseModelos(Id),
	CONSTRAINT FK_Fases_FaseStatus_Id FOREIGN KEY(Status) REFERENCES FaseStatus(Id)
  );
  CREATE INDEX Index_Fases_Processos_Empresas_Ordem ON Fases(EmpresaId, ProcessoId, Ordem);
END

-- PaginaRespostas
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='PaginaRespostas')
BEGIN
  CREATE TABLE PaginaRespostas(
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId        INT NOT NULL,
    FaseId           INT NOT NULL,
    PaginaModeloId   INT NOT NULL,
    CriadoEm         DATETIME2 NOT NULL CONSTRAINT DF_PaginaRespostas_CriadoEm DEFAULT(SYSDATETIME()),
    CONSTRAINT FK_PaginaRespostas_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_PaginaRespostas_Fases_Id FOREIGN KEY(FaseId) REFERENCES Fases(Id),
    CONSTRAINT FK_PaginaRespostas_PaginaModelos_Id FOREIGN KEY(PaginaModeloId) REFERENCES PaginaModelos(Id)
  );
  CREATE INDEX Index_PaginaRespostas_PaginaModelos_Empresas ON PaginaRespostas(EmpresaId, FaseId, PaginaModeloId);
END

-- CampoRespostas
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='CampoRespostas')
BEGIN
  CREATE TABLE CampoRespostas(
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId         INT NOT NULL,
    PaginaRespostaId  INT NOT NULL,
    CampoModeloId 	  INT NOT NULL,
    ValorTexto        NVARCHAR(MAX) NULL,
    ValorNumero    	  DECIMAL(18,4) NULL,
    ValorData      	  DATETIME2 NULL,
    ValorBool         BIT NULL,
    CONSTRAINT FK_CampoRespostas_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_CampoRespostas_PaginaRespostas_Id FOREIGN KEY(PaginaRespostaId) REFERENCES PaginaRespostas(Id),
    CONSTRAINT FK_CampoRespostas_CampoModelos_Id FOREIGN KEY(CampoModeloId) REFERENCES CampoModelos(Id)
  );
  CREATE INDEX Index_CampoRespostas_PaginaRespostas_CampoModelos_Empresas ON CampoRespostas(EmpresaId, PaginaRespostaId, CampoModeloId);
END
