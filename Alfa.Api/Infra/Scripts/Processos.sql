IF NOT EXISTS(SELECT 1 FROM sys.objects WHERE name = 'ProcessInstances' AND type = 'U')
BEGIN
  CREATE TABLE dbo.ProcessInstances(
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Titulo        NVARCHAR(200) NOT NULL,
    Status        NVARCHAR(40)  NOT NULL DEFAULT('EmAndamento'),
    ProgressoPct  INT           NOT NULL DEFAULT(0),
    EmpresaId     INT           NOT NULL,
    CriadoEm      DATETIME2     NOT NULL DEFAULT(SYSDATETIME())
  );
  CREATE INDEX IX_ProcessInstances_Empresa ON dbo.ProcessInstances(EmpresaId, Id DESC);
END