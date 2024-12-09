using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Auth.API.Models;
using Microsoft.EntityFrameworkCore;
using Auth.API.Constants;

namespace Auth.API.Account.RegisterUser
{
    public record RegisterUserCommand(
        string Name,
        string Email,
        string Password,
        IFormFile Image
    ) : IRequest<RegisterUserResult>;

    public record RegisterUserResult(
        bool Success,
        IEnumerable<string> Errors
    );

    internal class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IFileHandler _fileHandler;
        private readonly AppDbContext _db;

        public RegisterUserCommandHandler(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IFileHandler fileHandler,
            AppDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _fileHandler = fileHandler;
            _db = db;
        }

        public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // Process image
            var (file, imageClaim) = await ProcessImageAsync(request.Image, cancellationToken);

            // Create user
            var user = new User
            {
                Name = request.Name,
                UserName = request.Email,
                Email = request.Email,
                FileId = file?.Id
            };

            var result = await CreateUserAsync(user, request.Password, imageClaim);
            
            if (!result.Succeeded)
            {
                return new RegisterUserResult(
                    Success: false,
                    Errors: result.Errors.Select(e => e.Description)
                );
            }

            // Assign role
            var userRole = await _roleManager.Roles.FirstOrDefaultAsync(x => x.Name == Roles.RoleUser, cancellationToken);
            
            if (userRole != null)
            {
                await _userManager.AddToRoleAsync(user, userRole.Name);
            }

            return new RegisterUserResult(Success: true, Errors: []);
        }

        private async Task<(AppFile File, Claim ImageClaim)> ProcessImageAsync(IFormFile image, CancellationToken cancellationToken)
        {
            if (image == null)
            {
                return (null, null);
            }

            var filename = await _fileHandler.UploadAsync(image);

            var file = new AppFile
            {
                Name = filename,
                Path = _fileHandler.GeneratePath(filename)
            };

            _db.Files.Add(file);
            
            await _db.SaveChangesAsync(cancellationToken);

            var imageClaim = new Claim("Image", file.S3Path);
            
            return (file, imageClaim);
        }

        private async Task<IdentityResult> CreateUserAsync(User user, string password, Claim imageClaim)
        {
            var result = await _userManager.CreateAsync(user, password);
            
            if (!result.Succeeded)
            {
                return result;
            }

            if (imageClaim != null)
            {
                await _userManager.AddClaimAsync(user, imageClaim);
            }

            await _userManager.AddClaimsAsync(user,
            [
                new Claim("Name", user.Name)
            ]);

            return result;
        }
    }
}
