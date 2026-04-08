using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Repositories;
using HoneypotTrack.Test.Helpers;
using HonypotTrack.Application.Services;
using HonypotTrack.Application.Dtos.Cuenta;
using HonypotTrack.Application.Helpers;

namespace HoneypotTrack.Test.Services;

[TestClass]
public class CuentaServiceTests
{
    private AppDbContext _context = null!;
    private TestDataContextProvider _contextProvider = null!;
    private UnitOfWork _unitOfWork = null!;
    private CuentaService _cuentaService = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext testContext)
    {
        _ = AutoMapperHelper.Instance;
    }

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _contextProvider = new TestDataContextProvider(_context);
        _unitOfWork = new UnitOfWork(_contextProvider);
        _cuentaService = new CuentaService(_unitOfWork);

        // Seed: Crear usuario para las pruebas
        _context.Usuarios.Add(new Usuario
        {
            UserId = 1,
            FullName = "Test User",
            Email = "test@example.com"
        });
        _context.SaveChanges();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    [TestCategory("Cuenta")]
    public async Task CreateAsync_ShouldCreateCuenta_WhenValidData()
    {
        // Arrange
        var dto = new CuentaCreateDto
        {
            UserId = 1,
            AccountName = "Mi Cuenta",
            Currency = "USD"
        };

        // Act
        var result = await _cuentaService.CreateAsync(dto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("Mi Cuenta", result.Data.AccountName);
        Assert.AreEqual("USD", result.Data.Currency);
    }

    [TestMethod]
    [TestCategory("Cuenta")]
    public async Task CreateAsync_ShouldFail_WhenUserNotExists()
    {
        // Arrange
        var dto = new CuentaCreateDto
        {
            UserId = 999, // Usuario que no existe
            AccountName = "Cuenta",
            Currency = "USD"
        };

        // Act
        var result = await _cuentaService.CreateAsync(dto);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Message!.Contains("usuario no existe"));
    }

    [TestMethod]
    [TestCategory("Cuenta")]
    public async Task GetByUserIdAsync_ShouldReturnUserAccounts()
    {
        // Arrange
        await _cuentaService.CreateAsync(new CuentaCreateDto { UserId = 1, AccountName = "Cuenta 1", Currency = "USD" });
        await _cuentaService.CreateAsync(new CuentaCreateDto { UserId = 1, AccountName = "Cuenta 2", Currency = "EUR" });

        // Act
        var result = await _cuentaService.GetByUserIdAsync(1);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.Data!.Count());
    }

    [TestMethod]
    [TestCategory("Cuenta")]
    public async Task GetByIdAsync_ShouldReturnCuenta_WhenExists()
    {
        // Arrange
        var createResult = await _cuentaService.CreateAsync(new CuentaCreateDto
        {
            UserId = 1,
            AccountName = "Test",
            Currency = "USD"
        });

        // Act
        var result = await _cuentaService.GetByIdAsync(createResult.Data!.AccountId);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Test", result.Data!.AccountName);
    }

    [TestMethod]
    [TestCategory("Cuenta")]
    public async Task UpdateAsync_ShouldUpdateCuenta()
    {
        // Arrange
        var createResult = await _cuentaService.CreateAsync(new CuentaCreateDto
        {
            UserId = 1,
            AccountName = "Original",
            Currency = "USD"
        });

        var updateDto = new CuentaUpdateDto
        {
            AccountId = createResult.Data!.AccountId,
            UserId = 1,
            AccountName = "Actualizada",
            Currency = "EUR"
        };

        // Act
        var result = await _cuentaService.UpdateAsync(updateDto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Actualizada", result.Data!.AccountName);
        Assert.AreEqual("EUR", result.Data.Currency);
    }
}
