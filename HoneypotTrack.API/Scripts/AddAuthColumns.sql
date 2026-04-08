-- Script para agregar columnas de autenticación a la tabla Usuarios
-- Ejecutar en SQL Server

USE app_tesis;
GO

-- Agregar columna PasswordHash si no existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'empresa.Usuarios') AND name = 'passwordhash')
BEGIN
    ALTER TABLE empresa.Usuarios
    ADD passwordhash NVARCHAR(256) NULL;
    PRINT 'Columna passwordhash agregada';
END
ELSE
BEGIN
    PRINT 'Columna passwordhash ya existe';
END
GO

-- Agregar columna RefreshToken si no existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'empresa.Usuarios') AND name = 'refreshtoken')
BEGIN
    ALTER TABLE empresa.Usuarios
    ADD refreshtoken NVARCHAR(256) NULL;
    PRINT 'Columna refreshtoken agregada';
END
ELSE
BEGIN
    PRINT 'Columna refreshtoken ya existe';
END
GO

-- Agregar columna RefreshTokenExpiry si no existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'empresa.Usuarios') AND name = 'refreshtokenexpiry')
BEGIN
    ALTER TABLE empresa.Usuarios
    ADD refreshtokenexpiry DATETIME2 NULL;
    PRINT 'Columna refreshtokenexpiry agregada';
END
ELSE
BEGIN
    PRINT 'Columna refreshtokenexpiry ya existe';
END
GO

-- Crear índice para búsquedas por email (si no existe)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Usuarios_Email' AND object_id = OBJECT_ID('empresa.Usuarios'))
BEGIN
    CREATE INDEX IX_Usuarios_Email ON empresa.Usuarios(email);
    PRINT 'Índice IX_Usuarios_Email creado';
END
GO

PRINT 'Script de autenticación completado';
GO
