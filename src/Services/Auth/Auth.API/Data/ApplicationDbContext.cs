using Auth.API.Data.Seeders;
using Auth.API.Models;
using Auth.API.Services.ImageHandler;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Auth.API.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private readonly IServiceProvider _serviceProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider) : base(options)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            
            InitialSeeder.SeedAdmin(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            ChangeTracker.DetectChanges();
            
            var entities = ChangeTracker.Entries()
                        .Where(t => t.State == EntityState.Deleted)
                        .Select(t => t.Entity)
                        .ToArray();

            var imageService = _serviceProvider.GetRequiredService<IImageService>();

            foreach (var entity in entities)
            {
                if (entity is Image image)
                {
                    var file = entity as Image;
                    if (!string.IsNullOrWhiteSpace(file.PublicId))
                    {
                        await imageService.DeleteImageAsync(file.PublicId);
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        public DbSet<Image> Images { get; set; }
    }
}
