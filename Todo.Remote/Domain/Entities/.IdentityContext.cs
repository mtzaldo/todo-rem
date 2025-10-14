using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Todo.Remote.Domain.Entities;

public class IdentityContext : IdentityDbContext<IdentityUser>
{
    private readonly IConfiguration configuration;
    public IdentityContext(
        IConfiguration configuration,
        DbContextOptions<IdentityContext> options) :
        base(options)
    {
        this.configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(this.configuration.GetConnectionString("DefaultConnection"));
    }
}