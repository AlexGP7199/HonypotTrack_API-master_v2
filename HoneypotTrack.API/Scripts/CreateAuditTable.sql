-- Script para crear las tablas de Auditoría en SQL Server
-- Ejecutar en la base de datos: app_tesis

USE app_tesis
GO

-- =============================================
-- TABLA 1: AuditLogs (Auditoría de Requests HTTP)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.AuditLogs (
        AuditLogId INT IDENTITY(1,1) PRIMARY KEY,
        
        -- Información de la petición
        HttpMethod VARCHAR(10) NOT NULL,
        RequestUrl VARCHAR(500) NOT NULL,
        RequestPath VARCHAR(300) NOT NULL,
        QueryString VARCHAR(1000) NULL,
        RequestBody NVARCHAR(MAX) NULL,
        ResponseBody NVARCHAR(MAX) NULL,
        StatusCode INT NOT NULL,
        
        -- Información del cliente
        IpAddress VARCHAR(50) NULL,
        MacAddress VARCHAR(50) NULL,
        UserAgent VARCHAR(500) NULL,
        Browser VARCHAR(100) NULL,
        BrowserVersion VARCHAR(50) NULL,
        OperatingSystem VARCHAR(100) NULL,
        DeviceType VARCHAR(50) NULL,
        
        -- Información del usuario
        UserId INT NULL,
        UserName VARCHAR(100) NULL,
        UserEmail VARCHAR(100) NULL,
        
        -- Información de la acción
        ActionType VARCHAR(50) NULL,
        EntityName VARCHAR(100) NULL,
        EntityId VARCHAR(50) NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        ChangedColumns VARCHAR(500) NULL,
        
        -- Información de rendimiento
        ExecutionTimeMs BIGINT NOT NULL DEFAULT 0,
        RequestSize BIGINT NULL,
        ResponseSize BIGINT NULL,
        
        -- Información adicional
        ServerName VARCHAR(100) NULL,
        Environment VARCHAR(50) NULL,
        CorrelationId VARCHAR(50) NULL,
        SessionId VARCHAR(100) NULL,
        Referer VARCHAR(500) NULL,
        Origin VARCHAR(200) NULL,
        IsSuccessful BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(MAX) NULL,
        ExceptionDetails NVARCHAR(MAX) NULL,
        
        -- Timestamps
        Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LocalTimestamp DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    -- Índices para AuditLogs
    CREATE INDEX IX_AuditLogs_Timestamp ON empresa.AuditLogs(Timestamp);
    CREATE INDEX IX_AuditLogs_HttpMethod ON empresa.AuditLogs(HttpMethod);
    CREATE INDEX IX_AuditLogs_RequestPath ON empresa.AuditLogs(RequestPath);
    CREATE INDEX IX_AuditLogs_UserId ON empresa.AuditLogs(UserId);
    CREATE INDEX IX_AuditLogs_IpAddress ON empresa.AuditLogs(IpAddress);
    CREATE INDEX IX_AuditLogs_ActionType ON empresa.AuditLogs(ActionType);
    CREATE INDEX IX_AuditLogs_EntityName ON empresa.AuditLogs(EntityName);
    CREATE INDEX IX_AuditLogs_CorrelationId ON empresa.AuditLogs(CorrelationId);
    CREATE INDEX IX_AuditLogs_StatusCode ON empresa.AuditLogs(StatusCode);
    CREATE INDEX IX_AuditLogs_IsSuccessful ON empresa.AuditLogs(IsSuccessful);

    PRINT 'Tabla AuditLogs creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla AuditLogs ya existe';
END
GO

-- =============================================
-- TABLA 2: AuditoriaEntidades (Auditoría de cambios en BD)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditoriaEntidades' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.AuditoriaEntidades (
        AuditoriaId INT IDENTITY(1,1) PRIMARY KEY,
        
        -- Información de la entidad
        NombreTabla VARCHAR(100) NOT NULL,
        LlavePrimaria VARCHAR(50) NOT NULL,
        Accion VARCHAR(20) NOT NULL, -- Added, Modified, Deleted
        
        -- Valores
        ValorAnterior NVARCHAR(MAX) NULL,
        ValorNuevo NVARCHAR(MAX) NULL,
        ColumnasCambiadas VARCHAR(500) NULL,
        
        -- Información del usuario
        UsuarioId INT NULL,
        UsuarioNombre VARCHAR(100) NULL,
        UsuarioEmail VARCHAR(100) NULL,
        
        -- Información de la petición
        IpAddress VARCHAR(50) NULL,
        Path VARCHAR(300) NULL,
        MetodoHttp VARCHAR(10) NULL,
        UserAgent VARCHAR(500) NULL,
        CorrelationId VARCHAR(50) NULL,
        
        -- Timestamps
        FechaCambio DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FechaCambioLocal DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    -- Índices para AuditoriaEntidades
    CREATE INDEX IX_AuditoriaEntidades_NombreTabla ON empresa.AuditoriaEntidades(NombreTabla);
    CREATE INDEX IX_AuditoriaEntidades_LlavePrimaria ON empresa.AuditoriaEntidades(LlavePrimaria);
    CREATE INDEX IX_AuditoriaEntidades_Accion ON empresa.AuditoriaEntidades(Accion);
    CREATE INDEX IX_AuditoriaEntidades_UsuarioId ON empresa.AuditoriaEntidades(UsuarioId);
    CREATE INDEX IX_AuditoriaEntidades_FechaCambio ON empresa.AuditoriaEntidades(FechaCambio);
    CREATE INDEX IX_AuditoriaEntidades_CorrelationId ON empresa.AuditoriaEntidades(CorrelationId);

    PRINT 'Tabla AuditoriaEntidades creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla AuditoriaEntidades ya existe';
END
GO

-- =============================================
-- PROCEDIMIENTO: Limpiar logs antiguos
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CleanOldAuditLogs' AND schema_id = SCHEMA_ID('empresa'))
    DROP PROCEDURE empresa.sp_CleanOldAuditLogs
GO

CREATE PROCEDURE empresa.sp_CleanOldAuditLogs
    @DaysToKeep INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@DaysToKeep, GETUTCDATE());
    DECLARE @DeletedAuditLogs INT;
    DECLARE @DeletedAuditoriaEntidades INT;
    
    -- Limpiar AuditLogs
    DELETE FROM empresa.AuditLogs
    WHERE Timestamp < @CutoffDate;
    SET @DeletedAuditLogs = @@ROWCOUNT;
    
    -- Limpiar AuditoriaEntidades
    DELETE FROM empresa.AuditoriaEntidades
    WHERE FechaCambio < @CutoffDate;
    SET @DeletedAuditoriaEntidades = @@ROWCOUNT;
    
    PRINT CONCAT('AuditLogs eliminados: ', @DeletedAuditLogs);
    PRINT CONCAT('AuditoriaEntidades eliminados: ', @DeletedAuditoriaEntidades);
END
GO

-- =============================================
-- VISTA: Resumen de AuditLogs
-- =============================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AuditLogsSummary' AND schema_id = SCHEMA_ID('empresa'))
    DROP VIEW empresa.vw_AuditLogsSummary
GO

CREATE VIEW empresa.vw_AuditLogsSummary
AS
SELECT 
    AuditLogId,
    HttpMethod,
    RequestPath,
    StatusCode,
    ActionType,
    EntityName,
    EntityId,
    IpAddress,
    Browser,
    OperatingSystem,
    DeviceType,
    UserName,
    ExecutionTimeMs,
    IsSuccessful,
    LocalTimestamp
FROM empresa.AuditLogs
GO

-- =============================================
-- VISTA: Resumen de AuditoriaEntidades
-- =============================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AuditoriaEntidadesSummary' AND schema_id = SCHEMA_ID('empresa'))
    DROP VIEW empresa.vw_AuditoriaEntidadesSummary
GO

CREATE VIEW empresa.vw_AuditoriaEntidadesSummary
AS
SELECT 
    AuditoriaId,
    NombreTabla,
    LlavePrimaria,
    Accion,
    ColumnasCambiadas,
    UsuarioNombre,
    IpAddress,
    Path,
    MetodoHttp,
    FechaCambioLocal
FROM empresa.AuditoriaEntidades
GO

PRINT 'Script de auditoría ejecutado correctamente';
