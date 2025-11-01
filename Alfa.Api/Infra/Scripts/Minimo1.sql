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
    Titulo     NVARCHAR(100) NOT NULL,
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
    Mascara         NVARCHAR(50) NULL,
    Ajuda           NVARCHAR(250) NULL,
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
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId INT NOT NULL,
    Status    NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_ProcessoStatus_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
  );
  CREATE INDEX Index_ProcessoStatus_Empresas ON ProcessoStatus(EmpresaId, Id);
END

-- Processos (instâncias)
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='Processos')
BEGIN
  CREATE TABLE Processos(
    Id                    INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId             INT NOT NULL,
    Titulo                NVARCHAR(200) NOT NULL,
    StatusId              INT NOT NULL,
    PorcentagemProgresso  INT NOT NULL CONSTRAINT DF_Processos_PorcentagemProgresso DEFAULT(0),
    CriadoEm              DATETIME2 NOT NULL CONSTRAINT DF_Processos_CriadoEm DEFAULT(SYSDATETIME()),
    CONSTRAINT FK_Processos_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_Processos_ProcessoStatus_Id FOREIGN KEY(StatusId) REFERENCES ProcessoStatus(Id)
  );
  CREATE INDEX Index_Processos_Empresas ON Processos(EmpresaId, Id DESC);
END

-- FaseStatus
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FaseStatus')
BEGIN
  CREATE TABLE FaseStatus(
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId INT NOT NULL,
    Status    NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_FaseStatus_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
  );
  CREATE INDEX Index_FaseStatus_Empresas ON FaseStatus(EmpresaId, Id);
END

-- FaseInstancias
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FaseInstancias')
BEGIN
  CREATE TABLE FaseInstancias(
    Id                    INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId             INT NOT NULL,
    ProcessoId            INT NOT NULL,
    FaseModeloId          INT NOT NULL,
    Titulo                NVARCHAR(100) NOT NULL,
    Ordem                 INT NOT NULL,
    StatusId              INT NOT NULL,
    PorcentagemProgresso  INT NOT NULL CONSTRAINT DF_FaseInstancias_PorcentagemProgresso DEFAULT(0),
    CriadoEm              DATETIME2 NOT NULL CONSTRAINT DF_FaseInstancias_CriadoEm DEFAULT(SYSDATETIME()),
    CONSTRAINT FK_FaseInstancias_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_FaseInstancias_Processos_Id FOREIGN KEY(ProcessoId) REFERENCES Processos(Id),
    CONSTRAINT FK_FaseInstancias_FaseModelos_Id FOREIGN KEY(FaseModeloId) REFERENCES FaseModelos(Id),
    CONSTRAINT FK_FaseInstancias_FaseStatus_Id FOREIGN KEY(StatusId) REFERENCES FaseStatus(Id)
  );
  CREATE INDEX Index_FaseInstancias_Processos_Empresas_Ordem ON FaseInstancias(EmpresaId, ProcessoId, Ordem);
END

-- PaginaInstancias
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='PaginaInstancias')
BEGIN
  CREATE TABLE PaginaInstancias(
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId        INT NOT NULL,
    FaseInstanciaId  INT NOT NULL,
    PaginaModeloId   INT NOT NULL,
    Titulo           NVARCHAR(150) NOT NULL,
    Ordem            INT NOT NULL,
    Concluida        BIT NOT NULL CONSTRAINT DF_PaginaInstancias_Concluida DEFAULT(0),
    CONSTRAINT FK_PaginaInstancias_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_PaginaInstancias_FaseInstancias_Id FOREIGN KEY(FaseInstanciaId) REFERENCES FaseInstancias(Id),
    CONSTRAINT FK_PaginaInstancias_PaginaModelos_Id FOREIGN KEY(PaginaModeloId) REFERENCES PaginaModelos(Id)
  );
  CREATE INDEX Index_PaginaInstancias_FaseInstancias_Empresas_Ordem ON PaginaInstancias(EmpresaId, FaseInstanciaId, Ordem);
END

-- CampoInstancias (também armazena respostas)
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='CampoInstancias')
BEGIN
  CREATE TABLE CampoInstancias(
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    EmpresaId         INT NOT NULL,
    PaginaInstanciaId INT NOT NULL,
    CampoModeloId     INT NOT NULL,
    NomeCampo         NVARCHAR(120) NOT NULL,
    Rotulo            NVARCHAR(150) NOT NULL,
    Tipo              NVARCHAR(30) NOT NULL,
    Obrigatorio       BIT NOT NULL,
    Ordem             INT NOT NULL,
    Placeholder       NVARCHAR(150) NULL,
    Mascara           NVARCHAR(50) NULL,
    Ajuda             NVARCHAR(250) NULL,
    ValorTexto        NVARCHAR(MAX) NULL,
    ValorNumero       DECIMAL(18,4) NULL,
    ValorData         DATETIME2 NULL,
    ValorBool         BIT NULL,
    CONSTRAINT FK_CampoInstancias_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_CampoInstancias_PaginaInstancias_Id FOREIGN KEY(PaginaInstanciaId) REFERENCES PaginaInstancias(Id),
    CONSTRAINT FK_CampoInstancias_CampoModelos_Id FOREIGN KEY(CampoModeloId) REFERENCES CampoModelos(Id)
  );
  CREATE INDEX Index_CampoInstancias_PaginaInstancias_Empresas_Ordem ON CampoInstancias(EmpresaId, PaginaInstanciaId, Ordem);
END
