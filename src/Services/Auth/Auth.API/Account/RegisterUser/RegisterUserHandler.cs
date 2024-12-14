using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Auth.API.Models;
using Microsoft.EntityFrameworkCore;
using Auth.API.Constants;
using Auth.API.Services.ImageHandler;
using Auth.API.Data;

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
        private readonly IImageService _imageService;
        private readonly ApplicationDbContext _db;

        public RegisterUserCommandHandler(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IImageService imageService,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _imageService = imageService;
            _db = db;
        }

        public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // Process image
            var (image, imageClaim) = await ProcessImageAsync(request.Image, cancellationToken);

            // Create user
            var user = new User
            {
                Name = request.Name,
                UserName = request.Email,
                Email = request.Email,
                ImageId = image?.Id
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

        private async Task<(Image Image, Claim ImageClaim)> ProcessImageAsync(IFormFile imageToUpload, CancellationToken cancellationToken)
        {
            if (imageToUpload == null)
            {
                return (null, null);
            }

            var result = await _imageService.UploadImageAsync(imageToUpload);

            var image = new Image
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            _db.Images.Add(image);
            
            await _db.SaveChangesAsync(cancellationToken);

            var imageClaim = new Claim("Image", image.Url);
            
            return (image, imageClaim);
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
