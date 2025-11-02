-- ===============================
-- Tabela de Processos Padrão
-- ===============================
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name = 'ProcessoPadraoModelos')
BEGIN
    CREATE TABLE ProcessoPadraoModelos
    (
        Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId  INT NOT NULL,
        Titulo     NVARCHAR(200) NOT NULL,
        Descricao  NVARCHAR(500) NULL,
        CriadoEm   DATETIME2(0) NOT NULL CONSTRAINT DF_ProcessoPadraoModelos_CriadoEm DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_ProcessoPadraoModelos_Empresas_Id FOREIGN KEY(EmpresaId) REFERENCES Empresas(Id)
    );

    CREATE INDEX IX_ProcessoPadraoModelos_EmpresaId ON ProcessoPadraoModelos(EmpresaId, Titulo);
END;

IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name = 'ProcessoPadraoModeloFases')
BEGIN
    CREATE TABLE ProcessoPadraoModeloFases
    (
        ProcessoPadraoModeloId INT NOT NULL,
        FaseModeloId           INT NOT NULL,
        Ordem                  INT NOT NULL,
        CONSTRAINT PK_ProcessoPadraoModeloFases PRIMARY KEY(ProcessoPadraoModeloId, FaseModeloId),
        CONSTRAINT FK_ProcessoPadraoModeloFases_Padroes FOREIGN KEY(ProcessoPadraoModeloId) REFERENCES ProcessoPadraoModelos(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ProcessoPadraoModeloFases_FaseModelos FOREIGN KEY(FaseModeloId) REFERENCES FaseModelos(Id)
    );

    CREATE INDEX IX_ProcessoPadraoModeloFases_PadraoId ON ProcessoPadraoModeloFases(ProcessoPadraoModeloId, Ordem);
END;
