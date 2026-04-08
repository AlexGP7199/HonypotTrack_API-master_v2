-- Script para crear todas las tablas del sistema HoneypotTrack
-- Ejecutar en la base de datos: app_tesis

USE app_tesis
GO

-- =============================================
-- CREAR SCHEMA SI NO EXISTE
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'empresa')
BEGIN
    EXEC('CREATE SCHEMA empresa')
    PRINT 'Schema empresa creado'
END
GO

-- =============================================
-- TABLA: Usuarios
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.Usuarios (
        Userid INT IDENTITY(1,1) PRIMARY KEY,
        fullname NVARCHAR(100) NULL,
        email NVARCHAR(100) NOT NULL
    );

    CREATE UNIQUE INDEX IX_Usuarios_Email ON empresa.Usuarios(email);
    PRINT 'Tabla Usuarios creada'
END
GO

-- =============================================
-- TABLA: Cuenta
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cuenta' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.Cuenta (
        accountid INT IDENTITY(1,1) PRIMARY KEY,
        Userid INT NOT NULL,
        account_name NVARCHAR(50) NOT NULL,
        Currency NVARCHAR(3) NOT NULL DEFAULT 'USD',
        CONSTRAINT FK_Cuenta_Usuario FOREIGN KEY (Userid) REFERENCES empresa.Usuarios(Userid)
    );
    PRINT 'Tabla Cuenta creada'
END
GO

-- =============================================
-- TABLA: categoria
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'categoria' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.categoria (
        categoryid INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(50) NOT NULL,
        operationtype NVARCHAR(10) NOT NULL -- Ingreso o Egreso
    );
    PRINT 'Tabla categoria creada'
END
GO

-- =============================================
-- TABLA: Contacts
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Contacts' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.Contacts (
        contactsid INT IDENTITY(1,1) PRIMARY KEY,
        userid INT NOT NULL,
        name NVARCHAR(100) NOT NULL,
        type NVARCHAR(50) NOT NULL,
        taxid NVARCHAR(20) NOT NULL,
        CONSTRAINT FK_Contacts_Usuario FOREIGN KEY (userid) REFERENCES empresa.Usuarios(Userid)
    );
    PRINT 'Tabla Contacts creada'
END
GO

-- =============================================
-- TABLA: transacciones
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'transacciones' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.transacciones (
        transaccionid INT IDENTITY(1,1) PRIMARY KEY,
        accountid INT NOT NULL,
        categoryid INT NOT NULL,
        contactsid INT NULL,
        monto DECIMAL(18,2) NOT NULL,
        moneda NVARCHAR(10) NOT NULL DEFAULT 'USD',
        descripcion NVARCHAR(100) NULL,
        fecha DATETIME2 NULL,
        CONSTRAINT FK_Transacciones_Cuenta FOREIGN KEY (accountid) REFERENCES empresa.Cuenta(accountid),
        CONSTRAINT FK_Transacciones_Categoria FOREIGN KEY (categoryid) REFERENCES empresa.categoria(categoryid),
        CONSTRAINT FK_Transacciones_Contact FOREIGN KEY (contactsid) REFERENCES empresa.Contacts(contactsid)
    );
    PRINT 'Tabla transacciones creada'
END
GO

-- =============================================
-- TABLA: AuditLogs (Auditoría de Requests HTTP)
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
    CREATE INDEX IX_AuditLogs_HttpMethod ON empresa.AuditLogs(HttpMethod);
    CREATE INDEX IX_AuditLogs_RequestPath ON empresa.AuditLogs(RequestPath);
    CREATE INDEX IX_AuditLogs_CorrelationId ON empresa.AuditLogs(CorrelationId);
    PRINT 'Tabla AuditLogs creada'
END
GO

-- =============================================
-- TABLA: AuditoriaEntidades (Auditoría de cambios en BD)
-- =============================================
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

    CREATE INDEX IX_AuditoriaEntidades_NombreTabla ON empresa.AuditoriaEntidades(NombreTabla);
    CREATE INDEX IX_AuditoriaEntidades_FechaCambio ON empresa.AuditoriaEntidades(FechaCambio);
    PRINT 'Tabla AuditoriaEntidades creada'
END
GO

-- =============================================
-- DATOS INICIALES (Opcional)
-- =============================================

-- Insertar categorías de ejemplo
IF NOT EXISTS (SELECT * FROM empresa.categoria)
BEGIN
    INSERT INTO empresa.categoria (name, operationtype) VALUES 
    ('Sueldo', 'Ingreso'),
    ('Ventas', 'Ingreso'),
    ('Intereses', 'Ingreso'),
    ('Otros Ingresos', 'Ingreso'),
    ('Alimentos', 'Egreso'),
    ('Transporte', 'Egreso'),
    ('Servicios', 'Egreso'),
    ('Entretenimiento', 'Egreso'),
    ('Salud', 'Egreso'),
    ('Otros Gastos', 'Egreso');
    PRINT 'Categorías iniciales insertadas'
END
GO

PRINT '=========================================='
PRINT 'Script ejecutado correctamente'
PRINT '=========================================='
