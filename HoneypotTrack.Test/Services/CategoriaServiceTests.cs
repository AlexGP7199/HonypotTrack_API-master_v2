using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Repositories;
using HoneypotTrack.Test.Helpers;
using HonypotTrack.Application.Services;
using HonypotTrack.Application.Dtos.Categoria;
using HonypotTrack.Application.Helpers;

namespace HoneypotTrack.Test.Services;

[TestClass]
public class CategoriaServiceTests
{
    private AppDbContext _context = null!;
    private TestDataContextProvider _contextProvider = null!;
    private UnitOfWork _unitOfWork = null!;
    private CategoriaService _categoriaService = null!;

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
        _categoriaService = new CategoriaService(_unitOfWork);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    [TestCategory("Categoria")]
    public async Task CreateAsync_ShouldCreateCategoria_WhenValidIngreso()
    {
        // Arrange
        var dto = new CategoriaCreateDto
        {
            Name = "Sueldo",
            OperationType = "Ingreso"
        };

        // Act
        var result = await _categoriaService.CreateAsync(dto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("Sueldo", result.Data.Name);
        Assert.AreEqual("Ingreso", result.Data.OperationType);
    }

    [TestMethod]
    [TestCategory("Categoria")]
    public async Task CreateAsync_ShouldCreateCategoria_WhenValidEgreso()
    {
        // Arrange
        var dto = new CategoriaCreateDto
        {
            Name = "Alimentos",
            OperationType = "Egreso"
        };

        // Act
        var result = await _categoriaService.CreateAsync(dto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Egreso", result.Data!.OperationType);
    }

    [TestMethod]
    [TestCategory("Categoria")]
    public async Task CreateAsync_ShouldFail_WhenInvalidOperationType()
    {
        // Arrange
        var dto = new CategoriaCreateDto
        {
            Name = "Test",
            OperationType = "InvalidType"
        };

        // Act
        var result = await _categoriaService.CreateAsync(dto);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Message!.Contains("Ingreso") || result.Message.Contains("Egreso"));
    }

    [TestMethod]
    [TestCategory("Categoria")]
    public async Task GetByOperationTypeAsync_ShouldReturnOnlyIngresos()
    {
        // Arrange
        await _categoriaService.CreateAsync(new CategoriaCreateDto { Name = "Sueldo", OperationType = "Ingreso" });
        await _categoriaService.CreateAsync(new CategoriaCreateDto { Name = "Ventas", OperationType = "Ingreso" });
        await _categoriaService.CreateAsync(new CategoriaCreateDto { Name = "Alimentos", OperationType = "Egreso" });

        // Act
        var result = await _categoriaService.GetByOperationTypeAsync("Ingreso");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.Data!.Count());
        Assert.IsTrue(result.Data.All(c => c.OperationType == "Ingreso"));
    }

    [TestMethod]
    [TestCategory("Categoria")]
    public async Task GetAllWithoutPaginationAsync_ShouldReturnAllCategorias()
    {
        // Arrange
        await _categoriaService.CreateAsync(new CategoriaCreateDto { Name = "Cat 1", OperationType = "Ingreso" });
        await _categoriaService.CreateAsync(new CategoriaCreateDto { Name = "Cat 2", OperationType = "Egreso" });
        await _categoriaService.CreateAsync(new CategoriaCreateDto { Name = "Cat 3", OperationType = "Ingreso" });

        // Act
        var result = await _categoriaService.GetAllWithoutPaginationAsync();

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(3, result.Data!.Count());
    }

    [TestMethod]
    [TestCategory("Categoria")]
    public async Task GetByIdAsync_ShouldReturnCategoria_WhenExists()
    {
        // Arrange
        var createResult = await _categoriaService.CreateAsync(new CategoriaCreateDto
        {
            Name = "Test",
            OperationType = "Ingreso"
        });

        // Act
        var result = await _categoriaService.GetByIdAsync(createResult.Data!.CategoryId);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Test", result.Data!.Name);
    }

    [TestMethod]
    [TestCategory("Categoria")]
    public async Task UpdateAsync_ShouldUpdateCategoria()
    {
        // Arrange
        var createResult = await _categoriaService.CreateAsync(new CategoriaCreateDto
        {
            Name = "Original",
            OperationType = "Ingreso"
        });

        var updateDto = new CategoriaUpdateDto
        {
            CategoryId = createResult.Data!.CategoryId,
            Name = "Actualizado",
            OperationType = "Egreso"
        };

        // Act
        var result = await _categoriaService.UpdateAsync(updateDto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Actualizado", result.Data!.Name);
        Assert.AreEqual("Egreso", result.Data.OperationType);
    }
}
