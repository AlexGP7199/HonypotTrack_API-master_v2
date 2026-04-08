using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Repositories;
using HoneypotTrack.Test.Helpers;
using HonypotTrack.Application.Services;
using HonypotTrack.Application.Dtos.Usuario;
using HonypotTrack.Application.Helpers;

namespace HoneypotTrack.Test.Services;

[TestClass]
public class UsuarioServiceTests
{
    private AppDbContext _context = null!;
    private TestDataContextProvider _contextProvider = null!;
    private UnitOfWork _unitOfWork = null!;
    private UsuarioService _usuarioService = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext testContext)
    {
        // Inicializar AutoMapper
        _ = AutoMapperHelper.Instance;
    }

    [TestInitialize]
    public void Setup()
    {
        // Usar InMemory Database para pruebas unitarias
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _contextProvider = new TestDataContextProvider(_context);
        _unitOfWork = new UnitOfWork(_contextProvider);
        _usuarioService = new UsuarioService(_unitOfWork);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    [TestCategory("Usuario")]
    public async Task CreateAsync_ShouldCreateUsuario_WhenValidData()
    {
        // Arrange
        var dto = new UsuarioCreateDto
        {
            FullName = "Test User",
            Email = "test@example.com"
        };

        // Act
        var result = await _usuarioService.CreateAsync(dto);

        // Assert
        Assert.IsTrue(result.IsSuccess, $"Error: {result.Message}");
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("Test User", result.Data.FullName);
        Assert.AreEqual("test@example.com", result.Data.Email);
    }

    [TestMethod]
    [TestCategory("Usuario")]
    public async Task CreateAsync_ShouldFail_WhenDuplicateEmail()
    {
        // Arrange
        var dto1 = new UsuarioCreateDto { FullName = "User 1", Email = "duplicate@example.com" };
        var dto2 = new UsuarioCreateDto { FullName = "User 2", Email = "duplicate@example.com" };

        // Act
        await _usuarioService.CreateAsync(dto1);
        var result = await _usuarioService.CreateAsync(dto2);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Message!.Contains("email ya está registrado"));
    }

    [TestMethod]
    [TestCategory("Usuario")]
    public async Task GetByIdAsync_ShouldReturnUsuario_WhenExists()
    {
        // Arrange
        var createDto = new UsuarioCreateDto { FullName = "Test User", Email = "test@example.com" };
        var createResult = await _usuarioService.CreateAsync(createDto);

        // Act
        var result = await _usuarioService.GetByIdAsync(createResult.Data!.UserId);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("Test User", result.Data.FullName);
    }

    [TestMethod]
    [TestCategory("Usuario")]
    public async Task GetByIdAsync_ShouldFail_WhenNotExists()
    {
        // Act
        var result = await _usuarioService.GetByIdAsync(999);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Message!.Contains("no encontrado"));
    }

    [TestMethod]
    [TestCategory("Usuario")]
    public async Task UpdateAsync_ShouldUpdateUsuario_WhenValidData()
    {
        // Arrange
        var createDto = new UsuarioCreateDto { FullName = "Original Name", Email = "original@example.com" };
        var createResult = await _usuarioService.CreateAsync(createDto);

        var updateDto = new UsuarioUpdateDto
        {
            UserId = createResult.Data!.UserId,
            FullName = "Updated Name",
            Email = "updated@example.com"
        };

        // Act
        var result = await _usuarioService.UpdateAsync(updateDto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated Name", result.Data!.FullName);
        Assert.AreEqual("updated@example.com", result.Data.Email);
    }

    [TestMethod]
    [TestCategory("Usuario")]
    public async Task ExistsByEmailAsync_ShouldReturnTrue_WhenEmailExists()
    {
        // Arrange
        await _usuarioService.CreateAsync(new UsuarioCreateDto { FullName = "Test", Email = "exists@example.com" });

        // Act
        var result = await _usuarioService.ExistsByEmailAsync("exists@example.com");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Data);
    }

    [TestMethod]
    [TestCategory("Usuario")]
    public async Task ExistsByEmailAsync_ShouldReturnFalse_WhenEmailNotExists()
    {
        // Act
        var result = await _usuarioService.ExistsByEmailAsync("notexists@example.com");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.Data);
    }

    [TestMethod]
    [TestCategory("Usuario")]
    public async Task GetAllWithoutPaginationAsync_ShouldReturnAllUsuarios()
    {
        // Arrange
        await _usuarioService.CreateAsync(new UsuarioCreateDto { FullName = "User 1", Email = "user1@example.com" });
        await _usuarioService.CreateAsync(new UsuarioCreateDto { FullName = "User 2", Email = "user2@example.com" });
        await _usuarioService.CreateAsync(new UsuarioCreateDto { FullName = "User 3", Email = "user3@example.com" });

        // Act
        var result = await _usuarioService.GetAllWithoutPaginationAsync();

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(3, result.Data!.Count());
    }
}
