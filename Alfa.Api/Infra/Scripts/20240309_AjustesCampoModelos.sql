-- Ajuste para diferenciar campos de catálogo dos campos vinculados às páginas
IF COL_LENGTH('CampoModelos', 'EhCatalogo') IS NULL
BEGIN
    ALTER TABLE CampoModelos ADD EhCatalogo BIT NOT NULL CONSTRAINT DF_CampoModelos_EhCatalogo DEFAULT(0);
END;

-- Marca ao menos um campo por tipo como pertencente ao catálogo para cada empresa
;WITH Base AS (
    SELECT EmpresaId, Tipo, MIN(Id) AS IdMin
    FROM CampoModelos
    GROUP BY EmpresaId, Tipo
)
UPDATE cm
SET EhCatalogo = CASE WHEN base.IdMin = cm.Id THEN 1 ELSE EhCatalogo END
FROM CampoModelos cm
JOIN Base base
  ON base.EmpresaId = cm.EmpresaId
 AND base.Tipo = cm.Tipo;
