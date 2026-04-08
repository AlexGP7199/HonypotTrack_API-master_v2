-- =====================================================
-- HONEYPOT DATABASE SETUP
-- Base de datos señuelo para atrapar atacantes
-- Esquema compatible con las entidades de la aplicación
-- =====================================================

-- Crear la base de datos honeypot
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'app_honeypot')
BEGIN
    CREATE DATABASE app_honeypot;
END
GO

USE app_honeypot;
GO

-- Crear schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'empresa')
BEGIN
    EXEC('CREATE SCHEMA empresa');
END
GO

-- =====================================================
-- TABLA: Usuarios (MISMO ESQUEMA que app_tesis)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.Usuarios (
        Userid INT IDENTITY(1,1) PRIMARY KEY,
        fullname NVARCHAR(100) NOT NULL,
        email NVARCHAR(100) NOT NULL UNIQUE,
        passwordhash NVARCHAR(256),
        refreshtoken NVARCHAR(256),
        refreshtokenexpiry DATETIME2,
        -- Campos adicionales SOLO para honeypot (datos sensibles falsos)
        NumeroSeguroSocial NVARCHAR(20),
        NumeroTarjeta NVARCHAR(20),
        PIN NVARCHAR(10),
        Salario DECIMAL(18,2),
        Direccion NVARCHAR(500),
        Telefono NVARCHAR(20),
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        FechaActualizacion DATETIME2
    );
END
GO

-- =====================================================
-- TABLA: Cuenta (MISMO ESQUEMA que app_tesis)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cuenta' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.Cuenta (
        accountid INT IDENTITY(1,1) PRIMARY KEY,
        Userid INT NOT NULL,
        account_name NVARCHAR(50) NOT NULL,
        Currency NVARCHAR(3) DEFAULT 'USD',
        -- Campos adicionales SOLO para honeypot
        Saldo DECIMAL(18,2) DEFAULT 0,
        NumeroCuenta NVARCHAR(30),
        TipoCuenta NVARCHAR(50),
        NombreBanco NVARCHAR(100),
        NumeroRuta NVARCHAR(20),
        IBAN NVARCHAR(50),
        SWIFT NVARCHAR(20),
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        FechaActualizacion DATETIME2,
        FOREIGN KEY (Userid) REFERENCES empresa.Usuarios(Userid)
    );
END
GO

-- =====================================================
-- TABLA: categoria (MISMO ESQUEMA que app_tesis)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'categoria' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.categoria (
        categoryid INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(50) NOT NULL,
        operationtype NVARCHAR(10) NOT NULL,
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        FechaActualizacion DATETIME2
    );
END
GO

-- =====================================================
-- TABLA: Contacts (MISMO ESQUEMA que app_tesis)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Contacts' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.Contacts (
        contactsid INT IDENTITY(1,1) PRIMARY KEY,
        userid INT NOT NULL,
        name NVARCHAR(100) NOT NULL,
        type NVARCHAR(50) NOT NULL,
        Taxid NVARCHAR(20) NOT NULL,
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        FechaActualizacion DATETIME2,
        FOREIGN KEY (userid) REFERENCES empresa.Usuarios(Userid)
    );
END
GO

-- =====================================================
-- TABLA: transacciones (MISMO ESQUEMA que app_tesis)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'transacciones' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.transacciones (
        transaccionid INT IDENTITY(1,1) PRIMARY KEY,
        accountid INT NOT NULL,
        categoryid INT NOT NULL,
        contactsid INT,
        Monto DECIMAL(18,2) NOT NULL,
        moneda NVARCHAR(10) DEFAULT 'USD',
        Descripcion NVARCHAR(100),
        fecha DATETIME2 DEFAULT GETDATE(),
        -- Campos adicionales SOLO para honeypot
        CuentaDestino NVARCHAR(50),
        BancoDestino NVARCHAR(100),
        BeneficiarioNombre NVARCHAR(200),
        Referencia NVARCHAR(100),
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        FechaActualizacion DATETIME2,
        FOREIGN KEY (accountid) REFERENCES empresa.Cuenta(accountid),
        FOREIGN KEY (categoryid) REFERENCES empresa.categoria(categoryid)
    );
END
GO

-- =====================================================
-- TABLA: TarjetasCredito (SOLO HONEYPOT - muy atractiva)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TarjetasCredito' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.TarjetasCredito (
        TarjetaId INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioId INT NOT NULL,
        NumeroTarjeta NVARCHAR(20) NOT NULL,
        CVV NVARCHAR(5) NOT NULL,
        FechaExpiracion NVARCHAR(10) NOT NULL,
        NombreTitular NVARCHAR(200) NOT NULL,
        LimiteCredito DECIMAL(18,2),
        SaldoActual DECIMAL(18,2),
        TipoTarjeta NVARCHAR(50),
        Estado BIT DEFAULT 1,
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UsuarioId) REFERENCES empresa.Usuarios(Userid)
    );
END
GO

-- =====================================================
-- TABLA: ApiCredentials (SOLO HONEYPOT - muy atractiva)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiCredentials' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.ApiCredentials (
        CredentialId INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioId INT NOT NULL,
        ServiceName NVARCHAR(100) NOT NULL,
        ApiKey NVARCHAR(500) NOT NULL,
        ApiSecret NVARCHAR(500),
        AccessToken NVARCHAR(1000),
        RefreshToken NVARCHAR(1000),
        Endpoint NVARCHAR(500),
        Estado BIT DEFAULT 1,
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UsuarioId) REFERENCES empresa.Usuarios(Userid)
    );
END
GO

-- =====================================================
-- TABLA: HoneypotSessions (para tracking de atacantes)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HoneypotSessions' AND schema_id = SCHEMA_ID('empresa'))
BEGIN
    CREATE TABLE empresa.HoneypotSessions (
        SessionId INT IDENTITY(1,1) PRIMARY KEY,
        SessionToken NVARCHAR(MAX) NOT NULL,
        AttackerIp NVARCHAR(50) NOT NULL,
        InitialThreatType NVARCHAR(100),
        InitialPayload NVARCHAR(MAX),
        UserAgent NVARCHAR(500),
        AssignedUserId INT,
        IsActive BIT DEFAULT 1,
        StartTime DATETIME2 DEFAULT GETDATE(),
        LastActivity DATETIME2,
        EndTime DATETIME2,
        TotalRequests INT DEFAULT 0,
        TotalThreatsDetected INT DEFAULT 0
    );
END
GO

-- =====================================================
-- INSERTAR DATOS FALSOS ATRACTIVOS
-- =====================================================

-- Usuarios falsos con datos "sensibles"
SET IDENTITY_INSERT empresa.Usuarios ON;
INSERT INTO empresa.Usuarios (Userid, fullname, email, passwordhash, NumeroSeguroSocial, NumeroTarjeta, PIN, Salario, Direccion, Telefono)
VALUES 
    (1, 'Carlos Rodriguez (Admin)', 'admin@empresa.com', '$2a$11$fakehashadmin123456789', '123-45-6789', '4532015112830366', '1234', 150000.00, '123 Main St, New York, NY 10001', '+1-555-0100'),
    (2, 'Maria Gonzalez (CFO)', 'cfo@empresa.com', '$2a$11$fakehashedcfo2024abc', '234-56-7890', '5425233430109903', '5678', 250000.00, '456 Oak Ave, Los Angeles, CA 90001', '+1-555-0101'),
    (3, 'Juan Martinez (CEO)', 'ceo@empresa.com', '$2a$11$fakehashedceo_secure', '345-67-8901', '374245455400126', '9012', 500000.00, '789 Pine Rd, Miami, FL 33101', '+1-555-0102'),
    (4, 'Ana Lopez (HR)', 'hr@empresa.com', '$2a$11$fakehashedhr2024xyz', '456-78-9012', '6011000990139424', '3456', 85000.00, '321 Elm St, Chicago, IL 60601', '+1-555-0103'),
    (5, 'Pedro Sanchez (Developer)', 'developer@empresa.com', '$2a$11$fakehasheddev123qrs', '567-89-0123', '3566002020360505', '7890', 120000.00, '654 Maple Dr, Seattle, WA 98101', '+1-555-0104'),
    (6, 'Laura Fernandez (Accountant)', 'accountant@empresa.com', '$2a$11$fakehashedacc2024tuv', '678-90-1234', '4916338506082832', '2345', 95000.00, '987 Cedar Ln, Boston, MA 02101', '+1-555-0105'),
    (7, 'Roberto Garcia (Sales)', 'sales@empresa.com', '$2a$11$fakehashedsaleswxy', '789-01-2345', '5425230000004415', '6789', 75000.00, '147 Birch Blvd, Denver, CO 80201', '+1-555-0106'),
    (8, 'Sofia Diaz (Support)', 'support@empresa.com', '$2a$11$fakehashedsupportz12', '890-12-3456', '4532015112830374', '0123', 55000.00, '258 Spruce Way, Austin, TX 78701', '+1-555-0107'),
    (9, 'Miguel Torres (Investor)', 'investor@empresa.com', '$2a$11$fakehashedinvestor34', '901-23-4567', '374245455400134', '4567', 1000000.00, '369 Willow Ct, San Francisco, CA 94101', '+1-555-0108'),
    (10, 'Elena Ruiz (VP)', 'vp@empresa.com', '$2a$11$fakehashedvp2024567', '012-34-5678', '6011000990139432', '8901', 200000.00, '741 Aspen Pl, Portland, OR 97201', '+1-555-0109');
SET IDENTITY_INSERT empresa.Usuarios OFF;

-- Cuentas bancarias falsas con saldos atractivos
SET IDENTITY_INSERT empresa.Cuenta ON;
INSERT INTO empresa.Cuenta (accountid, Userid, account_name, Currency, Saldo, NumeroCuenta, TipoCuenta, NombreBanco, NumeroRuta, IBAN, SWIFT)
VALUES
    (1, 1, 'Cuenta Personal Admin', 'USD', 45000.00, '1234567890123456', 'Corriente', 'Bank of America', '021000021', 'US12BOFA12345678901234', 'BOFAUS3N'),
    (2, 2, 'Cuenta CFO Principal', 'USD', 125000.00, '2345678901234567', 'Ahorro', 'Chase Bank', '021000089', 'US23CHAS23456789012345', 'CHASUS33'),
    (3, 3, 'Cuenta Ejecutiva CEO', 'USD', 500000.00, '3456789012345678', 'Corriente', 'Wells Fargo', '121000248', 'US34WFBI34567890123456', 'WFBIUS6S'),
    (4, 3, 'Inversiones CEO', 'USD', 2500000.00, '4567890123456789', 'Inversiones', 'Goldman Sachs', '021000018', 'US45GSBI45678901234567', 'GSCMUS33'),
    (5, 4, 'Cuenta HR Operaciones', 'USD', 35000.00, '5678901234567890', 'Corriente', 'Citibank', '021000089', 'US56CITI56789012345678', 'CITIUS33'),
    (6, 5, 'Ahorros Developer', 'USD', 85000.00, '6789012345678901', 'Ahorro', 'TD Bank', '031101279', 'US67TDBA67890123456789', 'TDOMUS33'),
    (7, 6, 'Cuenta Contabilidad', 'USD', 55000.00, '7890123456789012', 'Corriente', 'PNC Bank', '043000096', 'US78PNCC78901234567890', 'PNCCUS33'),
    (8, 9, 'Portfolio Morgan Stanley', 'USD', 5000000.00, '8901234567890123', 'Inversiones', 'Morgan Stanley', '021000089', 'US89MSBI89012345678901', 'MSTCUS33'),
    (9, 9, 'Offshore Account', 'CHF', 10000000.00, '9012345678901234', 'Offshore', 'Swiss Bank Corp', '000000000', 'CH90SWIS90123456789012', 'UBSWCHZH'),
    (10, 10, 'Cuenta VP Operations', 'USD', 150000.00, '0123456789012345', 'Corriente', 'US Bank', '091000019', 'US01USBA01234567890123', 'USBKUS44');
SET IDENTITY_INSERT empresa.Cuenta OFF;

-- Categorías
SET IDENTITY_INSERT empresa.categoria ON;
INSERT INTO empresa.categoria (categoryid, name, operationtype)
VALUES
    (1, 'Nomina', 'Egreso'),
    (2, 'Ventas', 'Ingreso'),
    (3, 'Inversiones', 'Ingreso'),
    (4, 'Transferencias', 'Egreso'),
    (5, 'Servicios', 'Egreso'),
    (6, 'Dividendos', 'Egreso'),
    (7, 'Prestamos', 'Egreso'),
    (8, 'Cobros', 'Ingreso');
SET IDENTITY_INSERT empresa.categoria OFF;

-- Contactos falsos
SET IDENTITY_INSERT empresa.Contacts ON;
INSERT INTO empresa.Contacts (contactsid, userid, name, type, Taxid)
VALUES
    (1, 1, 'Amazon Web Services', 'Proveedor', '91-1234567'),
    (2, 2, 'Goldman Sachs Partners', 'Inversor', '13-5678901'),
    (3, 3, 'Swiss Holdings Ltd', 'Subsidiaria', 'CH-123456789'),
    (4, 3, 'Cayman Investments', 'Offshore', 'KY-987654321'),
    (5, 9, 'Private Equity Fund', 'Inversor', '98-7654321');
SET IDENTITY_INSERT empresa.Contacts OFF;

-- Transacciones recientes falsas (muy jugosas)
SET IDENTITY_INSERT empresa.transacciones ON;
INSERT INTO empresa.transacciones (transaccionid, accountid, categoryid, contactsid, Monto, moneda, Descripcion, fecha, CuentaDestino, BancoDestino, BeneficiarioNombre, Referencia)
VALUES
    (1, 3, 1, NULL, 50000.00, 'USD', 'Pago nomina marzo 2024', DATEADD(DAY, -5, GETDATE()), '9876543210', 'Bank of America', 'Empleados Corp', 'NOM-2024-03-001'),
    (2, 4, 3, 2, 125000.00, 'USD', 'Dividendos Q1 2024', DATEADD(DAY, -10, GETDATE()), NULL, NULL, NULL, 'DIV-2024-Q1'),
    (3, 8, 4, 5, 500000.00, 'USD', 'Inversion fondo privado', DATEADD(DAY, -3, GETDATE()), 'CH9300762011623852957', 'Credit Suisse', 'Private Equity Fund', 'INV-2024-001'),
    (4, 9, 4, 4, 2000000.00, 'CHF', 'Transferencia offshore', DATEADD(DAY, -1, GETDATE()), 'KY1234567890123456', 'Cayman National', 'Holdings Ltd', 'OFF-2024-001'),
    (5, 3, 2, NULL, 750000.00, 'USD', 'Venta proyecto enterprise', DATEADD(DAY, -7, GETDATE()), NULL, NULL, NULL, 'VTA-2024-ENT-001'),
    (6, 1, 5, 1, 2500.00, 'USD', 'Pago AWS servicios', DATEADD(DAY, -2, GETDATE()), '1234567890', 'AWS Inc', 'Amazon Web Services', 'AWS-2024-03'),
    (7, 2, 6, 2, 15000.00, 'USD', 'Pago dividendos accionistas', DATEADD(DAY, -4, GETDATE()), '5678901234', 'Goldman Sachs', 'Shareholders', 'DIV-OUT-001'),
    (8, 4, 7, 3, 100000.00, 'USD', 'Prestamo intercompania', DATEADD(DAY, -6, GETDATE()), '3456789012', 'Swiss Bank', 'Subsidiary LLC', 'LOAN-2024-001'),
    (9, 8, 3, NULL, 350000.00, 'USD', 'Retorno inversion crypto', DATEADD(DAY, -8, GETDATE()), NULL, NULL, NULL, 'CRYPTO-2024-001'),
    (10, 9, 4, NULL, 1500000.00, 'USD', 'Compra inmueble', DATEADD(DAY, -12, GETDATE()), '7890123456', 'Real Estate Bank', 'Luxury Properties Inc', 'REAL-2024-001');
SET IDENTITY_INSERT empresa.transacciones OFF;

-- Tarjetas de crédito falsas (MUY atractivas para atacantes)
INSERT INTO empresa.TarjetasCredito (UsuarioId, NumeroTarjeta, CVV, FechaExpiracion, NombreTitular, LimiteCredito, SaldoActual, TipoTarjeta)
VALUES
    (1, '4532015112830366', '123', '12/27', 'CARLOS RODRIGUEZ', 15000.00, 3500.00, 'Visa'),
    (2, '5425233430109903', '456', '03/28', 'MARIA GONZALEZ', 50000.00, 12000.00, 'MasterCard'),
    (3, '374245455400126', '7890', '06/26', 'JUAN MARTINEZ', 100000.00, 25000.00, 'American Express'),
    (3, '4916338506082832', '321', '09/27', 'JUAN MARTINEZ', 75000.00, 5000.00, 'Visa'),
    (4, '6011000990139424', '654', '11/28', 'ANA LOPEZ', 10000.00, 2500.00, 'Discover'),
    (5, '3566002020360505', '987', '02/27', 'PEDRO SANCHEZ', 20000.00, 8000.00, 'JCB'),
    (9, '4532015112830374', '111', '08/29', 'MIGUEL TORRES', 500000.00, 150000.00, 'Visa Infinite'),
    (9, '5425230000004415', '222', '04/28', 'MIGUEL TORRES', 250000.00, 75000.00, 'World Elite'),
    (10, '374245455400134', '3333', '07/27', 'ELENA RUIZ', 80000.00, 20000.00, 'American Express Gold');

-- Credenciales API falsas (muy atractivas)
INSERT INTO empresa.ApiCredentials (UsuarioId, ServiceName, ApiKey, ApiSecret, AccessToken, Endpoint)
VALUES
    (1, 'AWS', 'AKIAIOSFODNN7EXAMPLE', 'wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY', NULL, 'https://aws.amazon.com'),
    (1, 'Stripe', 'sk_live_51H7example1234567890abcdefghijklmnop', 'whsec_example123456789', NULL, 'https://api.stripe.com'),
    (2, 'PayPal', 'AYSq3RDGsmBLJE-otTkBtM-jBRd1TCQwFf9RGfwddNXWz0uFU9ztymylOhRS', 'EGnHDxD_qRPdaLdZz8iCr8N7_MzF-YHPTkjs6NKYQvQSBngp4PTTVWkPZRbL', NULL, 'https://api.paypal.com'),
    (3, 'Twilio', 'AC1234567890abcdef1234567890abcdef', '1234567890abcdef1234567890abcdef', NULL, 'https://api.twilio.com'),
    (3, 'SendGrid', 'SG.xxxxxxxxxxxxxxxxxxxx.yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy', NULL, NULL, 'https://api.sendgrid.com'),
    (5, 'GitHub', 'ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx', NULL, NULL, 'https://api.github.com'),
    (5, 'OpenAI', 'sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx', NULL, NULL, 'https://api.openai.com'),
    (9, 'Coinbase', 'organizations/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx/apiKeys/yyyyyyyy', 'ES256_PRIVATE_KEY_EXAMPLE_DO_NOT_USE', NULL, 'https://api.coinbase.com'),
    (9, 'Binance', 'vmPUZE6mv9SD5VNHk4HlWFsOr6aKE2zvsw0MuIgwCIPy6utIco14y7Ju91duEh8A', 'NhqPtmdSJYdKjVHjA7PZj4Mge3R5YNiP1e3UZjInClVN65XAbvqqM6A7H5fATj0j', NULL, 'https://api.binance.com');

PRINT '=====================================================';
PRINT '✅ Base de datos Honeypot creada exitosamente';
PRINT '🍯 Esquema compatible con entidades de la aplicación';
PRINT '⚠️ ADVERTENCIA: Todos los datos son FALSOS';
PRINT '=====================================================';
GO
