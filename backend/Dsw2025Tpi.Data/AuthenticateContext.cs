using Microsoft.AspNetCore.Identity;
using Dsw2025Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data
{
    public class AuthenticateContext : IdentityDbContext<AgroFlowUser>
    {
        public AuthenticateContext(DbContextOptions<AuthenticateContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AgroFlowUser>(b =>
            {
                b.ToTable("Usuarios");
                b.Property(user => user.IngenioId)
                    .IsRequired(false);
                b.Property(user => user.IsActive)
                    .HasDefaultValue(true)
                    .IsRequired();
            });
            builder.Entity<IdentityRole>(b => { b.ToTable("Roles"); });
            builder.Entity<IdentityUserRole<string>>(b => { b.ToTable("UsuariosRoles"); });
            builder.Entity<IdentityUserClaim<string>>(b => { b.ToTable("UsuariosClaims"); });
            builder.Entity<IdentityUserLogin<string>>(b => { b.ToTable("UsuariosLogins"); });
            builder.Entity<IdentityRoleClaim<string>>(b => { b.ToTable("RolesClaims"); });
            builder.Entity<IdentityUserToken<string>>(b => { b.ToTable("UsuariosTokens"); });
        }
    }
}
