using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Repositories;
using HoneypotTrack.Test.Helpers;
using HonypotTrack.Application.Services;
using HonypotTrack.Application.Dtos.Transaccion;
using HonypotTrack.Application.Helpers;

namespace HoneypotTrack.Test.Services;

[TestClass]
public class TransaccionServiceTests
{
    private AppDbContext _context = null!;
    private TestDataContextProvider _contextProvider = null!;
    private UnitOfWork _unitOfWork = null!;
    private TransaccionService _transaccionService = null!;

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
        _transaccionService = new TransaccionService(_unitOfWork);

        // Seed data
        var usuario = new Usuario { UserId = 1, FullName = "Test User", Email = "test@example.com" };
        var cuenta = new Cuenta { AccountId = 1, UserId = 1, AccountName = "Cuenta Test", Currency = "USD" };
        var categoriaIngreso = new Categoria { CategoryId = 1, Name = "Sueldo", OperationType = "Ingreso" };
        var categoriaEgreso = new Categoria { CategoryId = 2, Name = "Alimentos", OperationType = "Egreso" };

        _context.Usuarios.Add(usuario);
        _context.Cuentas.Add(cuenta);
        _context.Categorias.AddRange(categoriaIngreso, categoriaEgreso);
        _context.SaveChanges();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    [TestCategory("Transaccion")]
    public async Task CreateAsync_ShouldCreateTransaccion_WhenValidData()
    {
        // Arrange
        var dto = new TransaccionCreateDto
        {
            AccountId = 1,
            CategoryId = 1,
            Monto = 1500.00m,
            Moneda = "USD",
            Descripcion = "Pago mensual"
        };

        // Act
        var result = await _transaccionService.CreateAsync(dto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1500.00m, result.Data.Monto);
        Assert.AreEqual("USD", result.Data.Moneda);
    }

    [TestMethod]
    [TestCategory("Transaccion")]
    public async Task CreateAsync_ShouldFail_WhenCuentaNotExists()
    {
        // Arrange
        var dto = new TransaccionCreateDto
        {
            AccountId = 999,
            CategoryId = 1,
            Monto = 100m
        };

        // Act
        var result = await _transaccionService.CreateAsync(dto);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Message!.Contains("cuenta no existe"));
    }

    [TestMethod]
    [TestCategory("Transaccion")]
    public async Task CreateAsync_ShouldFail_WhenCategoriaNotExists()
    {
        // Arrange
        var dto = new TransaccionCreateDto
        {
            AccountId = 1,
            CategoryId = 999,
            Monto = 100m
        };

        // Act
        var result = await _transaccionService.CreateAsync(dto);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Message!.Contains("categoría no existe"));
    }

    [TestMethod]
    [TestCategory("Transaccion")]
    public async Task GetByAccountIdAsync_ShouldReturnAccountTransactions()
    {
        // Arrange
        await _transaccionService.CreateAsync(new TransaccionCreateDto { AccountId = 1, CategoryId = 1, Monto = 100m });
        await _transaccionService.CreateAsync(new TransaccionCreateDto { AccountId = 1, CategoryId = 2, Monto = 50m });

        // Act
        var result = await _transaccionService.GetByAccountIdAsync(1);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.Data!.Count());
    }

    [TestMethod]
    [TestCategory("Transaccion")]
    public async Task GetBalanceAsync_ShouldCalculateCorrectBalance()
    {
        // Arrange - Ingresos: 1000 + 500 = 1500, Egresos: 200 + 100 = 300
        await _transaccionService.CreateAsync(new TransaccionCreateDto { AccountId = 1, CategoryId = 1, Monto = 1000m }); // Ingreso
        await _transaccionService.CreateAsync(new TransaccionCreateDto { AccountId = 1, CategoryId = 1, Monto = 500m });  // Ingreso
        await _transaccionService.CreateAsync(new TransaccionCreateDto { AccountId = 1, CategoryId = 2, Monto = 200m });  // Egreso
        await _transaccionService.CreateAsync(new TransaccionCreateDto { AccountId = 1, CategoryId = 2, Monto = 100m });  // Egreso

        // Act
        var result = await _transaccionService.GetBalanceAsync(1);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1200m, result.Data); // 1500 - 300 = 1200
    }

    [TestMethod]
    [TestCategory("Transaccion")]
    public async Task GetTotalByOperationTypeAsync_ShouldReturnCorrectTotal()
    {
        // Arrange
        await _transaccionService.CreateAsync(new TransaccionCreateDto { AccountId = 1, CategoryId = 1, Monto = 1000m }); // Ingreso
        await _transaccionService.CreateAsync(new TransaccionCreateDto { AccountId = 1, CategoryId = 1, Monto = 500m });  // Ingreso

        // Act
        var result = await _transaccionService.GetTotalByOperationTypeAsync(1, "Ingreso");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1500m, result.Data);
    }

    [TestMethod]
    [TestCategory("Transaccion")]
    public async Task GetByIdAsync_ShouldReturnTransaccion_WhenExists()
    {
        // Arrange
        var createResult = await _transaccionService.CreateAsync(new TransaccionCreateDto
        {
            AccountId = 1,
            CategoryId = 1,
            Monto = 100m
        });

        // Act
        var result = await _transaccionService.GetByIdAsync(createResult.Data!.TransaccionId);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100m, result.Data!.Monto);
    }
}
