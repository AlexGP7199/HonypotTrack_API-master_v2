-- Script para agregar columnas de auditoría a las tablas existentes
-- Base de datos: app_tesis

USE app_tesis
GO

-- =============================================
-- AGREGAR COLUMNAS DE AUDITORÍA A TODAS LAS TABLAS
-- =============================================

-- USUARIOS
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.Usuarios') AND name = 'FechaCreacion')
BEGIN
    ALTER TABLE empresa.Usuarios ADD FechaCreacion DATETIME2 DEFAULT GETDATE();
    PRINT 'Columna FechaCreacion agregada a Usuarios'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.Usuarios') AND name = 'FechaActualizacion')
BEGIN
    ALTER TABLE empresa.Usuarios ADD FechaActualizacion DATETIME2 NULL;
    PRINT 'Columna FechaActualizacion agregada a Usuarios'
END
GO

-- CUENTA
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.Cuenta') AND name = 'FechaCreacion')
BEGIN
    ALTER TABLE empresa.Cuenta ADD FechaCreacion DATETIME2 DEFAULT GETDATE();
    PRINT 'Columna FechaCreacion agregada a Cuenta'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.Cuenta') AND name = 'FechaActualizacion')
BEGIN
    ALTER TABLE empresa.Cuenta ADD FechaActualizacion DATETIME2 NULL;
    PRINT 'Columna FechaActualizacion agregada a Cuenta'
END
GO

-- CATEGORIA
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.categoria') AND name = 'FechaCreacion')
BEGIN
    ALTER TABLE empresa.categoria ADD FechaCreacion DATETIME2 DEFAULT GETDATE();
    PRINT 'Columna FechaCreacion agregada a categoria'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.categoria') AND name = 'FechaActualizacion')
BEGIN
    ALTER TABLE empresa.categoria ADD FechaActualizacion DATETIME2 NULL;
    PRINT 'Columna FechaActualizacion agregada a categoria'
END
GO

-- CONTACTS
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.Contacts') AND name = 'FechaCreacion')
BEGIN
    ALTER TABLE empresa.Contacts ADD FechaCreacion DATETIME2 DEFAULT GETDATE();
    PRINT 'Columna FechaCreacion agregada a Contacts'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.Contacts') AND name = 'FechaActualizacion')
BEGIN
    ALTER TABLE empresa.Contacts ADD FechaActualizacion DATETIME2 NULL;
    PRINT 'Columna FechaActualizacion agregada a Contacts'
END
GO

-- TRANSACCIONES - Agregar PK si no existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.transacciones') AND name = 'transaccionid')
BEGIN
    ALTER TABLE empresa.transacciones ADD transaccionid INT IDENTITY(1,1) PRIMARY KEY;
    PRINT 'Columna transaccionid (PK) agregada a transacciones'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.transacciones') AND name = 'FechaCreacion')
BEGIN
    ALTER TABLE empresa.transacciones ADD FechaCreacion DATETIME2 DEFAULT GETDATE();
    PRINT 'Columna FechaCreacion agregada a transacciones'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('empresa.transacciones') AND name = 'FechaActualizacion')
BEGIN
    ALTER TABLE empresa.transacciones ADD FechaActualizacion DATETIME2 NULL;
    PRINT 'Columna FechaActualizacion agregada a transacciones'
END
GO

-- =============================================
-- ACTUALIZAR REGISTROS EXISTENTES CON FECHA ACTUAL
-- =============================================
UPDATE empresa.Usuarios SET FechaCreacion = GETDATE() WHERE FechaCreacion IS NULL;
UPDATE empresa.Cuenta SET FechaCreacion = GETDATE() WHERE FechaCreacion IS NULL;
UPDATE empresa.categoria SET FechaCreacion = GETDATE() WHERE FechaCreacion IS NULL;
UPDATE empresa.Contacts SET FechaCreacion = GETDATE() WHERE FechaCreacion IS NULL;
UPDATE empresa.transacciones SET FechaCreacion = GETDATE() WHERE FechaCreacion IS NULL;
GO

-- =============================================
-- CREAR TABLAS DE AUDITORÍA SI NO EXISTEN
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.AuditLogs (
        AuditLogId INT IDENTITY(1,1) PRIMARY KEY,
        HttpMethod VARCHAR(10) NOT NULL,
        RequestUrl VARCHAR(500) NOT NULL,
        RequestPath VARCHAR(300) NOT NULL,
        QueryString VARCHAR(1000) NULL,
        RequestBody NVARCHAR(MAX) NULL,
        ResponseBody NVARCHAR(MAX) NULL,
        StatusCode INT NOT NULL,
        IpAddress VARCHAR(50) NULL,
        MacAddress VARCHAR(50) NULL,
        UserAgent VARCHAR(500) NULL,
        Browser VARCHAR(100) NULL,
        BrowserVersion VARCHAR(50) NULL,
        OperatingSystem VARCHAR(100) NULL,
        DeviceType VARCHAR(50) NULL,
        UserId INT NULL,
        UserName VARCHAR(100) NULL,
        UserEmail VARCHAR(100) NULL,
        ActionType VARCHAR(50) NULL,
        EntityName VARCHAR(100) NULL,
        EntityId VARCHAR(50) NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        ChangedColumns VARCHAR(500) NULL,
        ExecutionTimeMs BIGINT NOT NULL DEFAULT 0,
        RequestSize BIGINT NULL,
        ResponseSize BIGINT NULL,
        ServerName VARCHAR(100) NULL,
        Environment VARCHAR(50) NULL,
        CorrelationId VARCHAR(50) NULL,
        SessionId VARCHAR(100) NULL,
        Referer VARCHAR(500) NULL,
        Origin VARCHAR(200) NULL,
        IsSuccessful BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(MAX) NULL,
        ExceptionDetails NVARCHAR(MAX) NULL,
        Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LocalTimestamp DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_AuditLogs_Timestamp ON empresa.AuditLogs(Timestamp);
    CREATE INDEX IX_AuditLogs_CorrelationId ON empresa.AuditLogs(CorrelationId);
    PRINT 'Tabla AuditLogs creada'
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditoriaEntidades' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.AuditoriaEntidades (
        AuditoriaId INT IDENTITY(1,1) PRIMARY KEY,
        NombreTabla VARCHAR(100) NOT NULL,
        LlavePrimaria VARCHAR(50) NOT NULL,
        Accion VARCHAR(20) NOT NULL,
        ValorAnterior NVARCHAR(MAX) NULL,
        ValorNuevo NVARCHAR(MAX) NULL,
        ColumnasCambiadas VARCHAR(500) NULL,
        UsuarioId INT NULL,
        UsuarioNombre VARCHAR(100) NULL,
        UsuarioEmail VARCHAR(100) NULL,
        IpAddress VARCHAR(50) NULL,
        Path VARCHAR(300) NULL,
        MetodoHttp VARCHAR(10) NULL,
        UserAgent VARCHAR(500) NULL,
        CorrelationId VARCHAR(50) NULL,
        FechaCambio DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FechaCambioLocal DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_AuditoriaEntidades_FechaCambio ON empresa.AuditoriaEntidades(FechaCambio);
    PRINT 'Tabla AuditoriaEntidades creada'
END
GO

PRINT '=========================================='
PRINT 'Columnas de auditoría agregadas correctamente'
PRINT '=========================================='
