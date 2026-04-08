using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HoneypotTrack.Infrastrcture.Persistences.Context;

namespace HoneypotTrack.Test.Infrastructure;

[TestClass]
public class DatabaseConnectionTests
{
    private static IConfiguration? _configuration;
    private static string? _connectionString;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        _connectionString = _configuration.GetConnectionString("DefaultConnection");
    }

    [TestMethod]
    [TestCategory("Database")]
    public void ConnectionString_ShouldNotBeNullOrEmpty()
    {
        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(_connectionString), 
            "La cadena de conexión no debe ser nula o vacía");
    }

    [TestMethod]
    [TestCategory("Database")]
    public void ConnectionString_ShouldContainCorrectDatabase()
    {
        // Assert
        Assert.IsTrue(_connectionString!.Contains("app_tesis"), 
            "La cadena de conexión debe apuntar a la base de datos 'app_tesis'");
    }

    [TestMethod]
    [TestCategory("Database")]
    public async Task CanConnect_ToDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Act & Assert
        await using var context = new AppDbContext(options);
        var canConnect = await context.Database.CanConnectAsync();

        Assert.IsTrue(canConnect, "Debe poder conectarse a la base de datos");
    }

    [TestMethod]
    [TestCategory("Database")]
    public async Task Database_ShouldHaveUsuariosTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Act
        await using var context = new AppDbContext(options);
        var usuarios = await context.Usuarios.Take(1).ToListAsync();

        // Assert - Si no lanza excepción, la tabla existe
        Assert.IsNotNull(usuarios);
    }

    [TestMethod]
    [TestCategory("Database")]
    public async Task Database_ShouldHaveCuentasTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Act
        await using var context = new AppDbContext(options);
        var cuentas = await context.Cuentas.Take(1).ToListAsync();

        // Assert
        Assert.IsNotNull(cuentas);
    }

    [TestMethod]
    [TestCategory("Database")]
    public async Task Database_ShouldHaveCategoriasTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Act
        await using var context = new AppDbContext(options);
        var categorias = await context.Categorias.Take(1).ToListAsync();

        // Assert
        Assert.IsNotNull(categorias);
    }

    [TestMethod]
    [TestCategory("Database")]
    public async Task Database_ShouldHaveContactsTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Act
        await using var context = new AppDbContext(options);
        var contacts = await context.Contacts.Take(1).ToListAsync();

        // Assert
        Assert.IsNotNull(contacts);
    }

    [TestMethod]
    [TestCategory("Database")]
    public async Task Database_ShouldHaveTransaccionesTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Act
        await using var context = new AppDbContext(options);
        var transacciones = await context.Transacciones.Take(1).ToListAsync();

        // Assert
        Assert.IsNotNull(transacciones);
    }

    [TestMethod]
    [TestCategory("Database")]
    public async Task Database_ShouldHaveAuditLogsTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Act
        await using var context = new AppDbContext(options);
        var auditLogs = await context.AuditLogs.Take(1).ToListAsync();

        // Assert
        Assert.IsNotNull(auditLogs);
    }

    [TestMethod]
    [TestCategory("Database")]
    public async Task Database_ShouldHaveAuditoriaEntidadesTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        // Act
        await using var context = new AppDbContext(options);
        var auditoriaEntidades = await context.AuditoriaEntidades.Take(1).ToListAsync();

        // Assert
        Assert.IsNotNull(auditoriaEntidades);
    }
}
