using Microsoft.EntityFrameworkCore;
using SWD_Group4.BusinessLogic.DTO;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.DataAccess.Context;
using SWD_Group4.DataAccess.Models;
using System.ComponentModel.DataAnnotations;

namespace SWD_Group4.BusinessLogic.Services;

public sealed class AuthService : IAuthService
{
    private const string DefaultRole = "Customer";
    private const string DefaultStatus = "Active";

    private readonly BookStoreContext _dbContext;
    private readonly EmailAddressAttribute _emailValidator = new();

    public AuthService(BookStoreContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegisterUserResultDto> registerUser(string name, string email, string password)
    {
        try
        {
            var validationError = await validateUserData(name, email, password);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return new RegisterUserResultDto
                {
                    IsSuccess = false,
                    Message = validationError
                };
            }

            var trimmedName = name.Trim();
            var trimmedEmail = email.Trim();

            var user = new User
            {
                Name = trimmedName,
                Email = trimmedEmail,
                Password = hashPassword(password),
                Role = DefaultRole,
                Status = DefaultStatus
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return new RegisterUserResultDto
            {
                IsSuccess = true,
                UserId = user.Id,
                Message = "Registration successful."
            };
        }
        catch (DbUpdateException)
        {
            // Covers unique email constraint in DB; also handles concurrent inserts.
            return new RegisterUserResultDto
            {
                IsSuccess = false,
                Message = "Username or Email already exists. Please try another."
            };
        }
        catch
        {
            return new RegisterUserResultDto
            {
                IsSuccess = false,
                Message = "System error: Unable to complete registration. Please try again later."
            };
        }
    }

    private async Task<string?> validateUserData(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Username is required.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        if (!_emailValidator.IsValid(email))
        {
            return "Incorrect email format";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (!IsPasswordComplex(password))
        {
            return "Password not meeting security requirements";
        }

        // BR-ACC-01 uniqueness
        var trimmedEmail = email.Trim();
        var trimmedName = name.Trim();

        var emailUnique = await checkEmailUnique(trimmedEmail);
        var usernameUnique = !await _dbContext.Users.AnyAsync(u => u.Name != null && u.Name == trimmedName);

        if (!emailUnique || !usernameUnique)
        {
            return "Username or Email already exists. Please try another.";
        }

        return null;
    }

    private async Task<bool> checkEmailUnique(string email)
    {
        return !await _dbContext.Users.AnyAsync(u => u.Email != null && u.Email == email);
    }

    private static string hashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public async Task<LoginResultDto> loginUser(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new LoginResultDto
                {
                    IsSuccess = false,
                    Message = "Missing required fields"
                };
            }

            if (!_emailValidator.IsValid(email))
            {
                return new LoginResultDto
                {
                    IsSuccess = false,
                    Message = "Incorrect email format"
                };
            }

            var trimmedEmail = email.Trim();
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email == trimmedEmail);

            if (user == null || string.IsNullOrWhiteSpace(user.Password))
            {
                return new LoginResultDto
                {
                    IsSuccess = false,
                    Message = "Invalid email or password."
                };
            }

            var ok = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if (!ok)
            {
                return new LoginResultDto
                {
                    IsSuccess = false,
                    Message = "Invalid email or password."
                };
            }

            if (!string.Equals(user.Status, DefaultStatus, StringComparison.OrdinalIgnoreCase))
            {
                return new LoginResultDto
                {
                    IsSuccess = false,
                    Message = "Account is inactive."
                };
            }

            return new LoginResultDto
            {
                IsSuccess = true,
                UserId = user.Id,
                Name = user.Name ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role ?? string.Empty,
                Message = "Login successful."
            };
        }
        catch
        {
            return new LoginResultDto
            {
                IsSuccess = false,
                Message = "System error: Unable to login. Please try again later."
            };
        }
    }

    private static bool IsPasswordComplex(string password)
    {
        if (password.Length < 8)
        {
            return false;
        }

        var hasUpper = false;
        var hasDigit = false;
        var hasSpecial = false;

        foreach (var ch in password)
        {
            if (char.IsUpper(ch))
            {
                hasUpper = true;
                continue;
            }

            if (char.IsDigit(ch))
            {
                hasDigit = true;
                continue;
            }

            if (!char.IsLetterOrDigit(ch))
            {
                hasSpecial = true;
            }
        }

        return hasUpper && hasDigit && hasSpecial;
    }
}
