using Microsoft.EntityFrameworkCore;
using sgspe.Common.Exceptions;
using sgspe.Data;
using sgspe.Models.Users;


namespace sgspe.Services.Users;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, PasswordHasher passwordHasher, ILogger<UserService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByNameAsync(string name)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Name.ToLower() == name.ToLower());
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> CreateAsync(CreateUserRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Creating user with email: {Email} and CompanyId: {CompanyId}",
                request.Email, request.CompanyId);

            // Log para verificar o CompanyId
            _logger.LogInformation("CompanyId received: {CompanyId}, Type: {Type}",
                request.CompanyId, request.CompanyId.GetType());

            // Verificar se existe ALGUMA company no banco
            var anyCompany = await _context.Companies.AnyAsync();
            _logger.LogInformation("Any company in database: {AnyCompany}", anyCompany);

            // Verificar se ESTA company existe
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == request.CompanyId);
            _logger.LogInformation("Company with ID {CompanyId} exists: {CompanyExists}",
                request.CompanyId, companyExists);

            if (!companyExists)
            {
                // Listar todas as companies disponíveis para debug
                var allCompanies = await _context.Companies.Select(c => new { c.Id, c.LegalName }).ToListAsync();
                _logger.LogWarning("Company not found. Available companies: {Companies}",
                    System.Text.Json.JsonSerializer.Serialize(allCompanies));

                throw new InvalidUserOperationException($"Company with ID {request.CompanyId} not found");
            }
            _logger.LogInformation("Creating user with email: {Email}", request.Email);
            var existingUser = await GetByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                throw new DuplicateEmailException($"Email {request.Email} already exists", ex: null!);
            }

            _logger.LogInformation("No duplicate email found, creating user object");


            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role = request.Role,
                CompanyId = request.CompanyId
            };
            _logger.LogInformation("User object created, adding to context");

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User saved to database, committing transaction");
            await transaction.CommitAsync();

            _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);
            return user;
        }
        catch (DuplicateEmailException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Database update error creating user: {Message}", ex.Message);

            if (IsUniqueConstraintViolation(ex))
            {
                throw new DuplicateEmailException("Email already exists", ex);
            }

            throw new InvalidUserOperationException($"Database error: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Unexpected error creating user: {Message}", ex.Message);
            throw new InvalidUserOperationException($"Error creating user: {ex.Message}");
        }

    }

    public async Task<User> UpdateAsync(string email, UpdateUserRequest request)
    {
        var user = await GetByEmailAsync(email);
        ArgumentNullException.ThrowIfNull(user, "User not found");

        if (!string.IsNullOrEmpty(request.Name) &&
        await _context.Users.AnyAsync(u =>
            u.Name.ToLower() == request.Name.ToLower() && u.Id != user.Id))

            throw new ArgumentException("Name already exists");
        user.Name = request.Name ?? user.Name;

        await _context.SaveChangesAsync();
        return user;
    }
    public async Task<User> UpdateCompanyAsync(string email, UpdateUserCompanyRequest request)
    {
        var user = await GetByEmailAsync(email);
        if (user is null)
            throw new ArgumentException("User not found");
        user.CompanyId = request.CompanyId;

        await _context.SaveChangesAsync();
        return user;
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<User?> LoginAsync(LoginUserRequest request)
    {
        var user = await GetByEmailAsync(request.Email);
        if (user == null || !user.IsActive) return null;

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return user;
    }
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteException)
        {
            // SQLite - erro 19 é CONSTRAINT, códigos específicos para UNIQUE e PRIMARY KEY
            return sqliteException.SqliteErrorCode == 19 &&
                   (sqliteException.SqliteExtendedErrorCode == 2067 ||
                    sqliteException.SqliteExtendedErrorCode == 1555);
        }

        // Se necessario adicionar suporte para outros bancos de dados, faça aqui
        return false;
    }
    // Helper para mapear User para UserResponse
    public static UserResponse MapToResponse(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.CompanyId,
        user.CreatedAt,
        user.LastLoginAt,
        user.IsActive,
        user.Role
    );
}