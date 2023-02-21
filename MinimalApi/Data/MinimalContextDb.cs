using Microsoft.EntityFrameworkCore;
using MinimalApi.Models;

namespace MinimalApi.Data
{
    public class MinimalContextDb : DbContext
    {
        public MinimalContextDb(DbContextOptions<MinimalContextDb> options) : base(options) { }

        public DbSet<Fornecedor> Fornecedores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Fornecedor>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Fornecedor>()
                .Property(e => e.Nome)
                .IsRequired()
                .HasColumnType("varchar(200)");

            modelBuilder.Entity<Fornecedor>()
                .Property(e => e.Documento)
                .IsRequired()
                .HasColumnType("varchar(14)");

            modelBuilder.Entity<Fornecedor>()
                .ToTable("Fornecedores");

            base.OnModelCreating(modelBuilder);
        }
        
    }
}
