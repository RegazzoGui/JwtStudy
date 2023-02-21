using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MinimalApi.Data;
using MinimalApi.Models;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("MinimalApi")));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");
        

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();

app.UseHttpsRedirection();

app.MapPost("/registro", async (
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppJwtSettings> appJwtSettings,
    RegisterUser registerUser) =>
    {
        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        var jwt = new JwtBuilder()
                        .WithUserManager(userManager)
                        .WithJwtSettings(appJwtSettings.Value)
                        .WithEmail(user.Email)
                        .WithJwtClaims()
                        .WithUserClaims()
                        .WithUserRoles()
                        .BuildUserResponse();

        return Results.Ok(jwt);

    })
    .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("RegistroUsuario")
        .WithTags("Usuario");

app.MapPost(";login", async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser loginUser) =>
{
    if (loginUser == null)
        return Results.BadRequest("Usuário não informado");

    if (!MiniValidator.TryValidate(loginUser, out var errors))
        return Results.ValidationProblem(errors);

    var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

    if (result.IsLockedOut)
        return Results.BadRequest("Usuário bloqueado");

    if (!result.Succeeded)
        return Results.BadRequest("Usuário ou senha inválidos");

    var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(loginUser.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

    return Results.Ok(jwt);

});

app.MapGet("/fornecedor", async (
    MinimalContextDb context) =>
    await context.Fornecedores.ToListAsync())
    .WithName("GetFornecedor")
    .WithTags("Fornecedor");

app.MapGet("/fornecedor/{id}", async (
    Guid id,
    MinimalContextDb context) =>
    await context.Fornecedores.FindAsync(id)
        is Fornecedor fornecedor
            ? Results.Ok(fornecedor)
            : Results.NotFound())
    .Produces<Fornecedor>(StatusCodes.Status200OK)
    .Produces<Fornecedor>(StatusCodes.Status404NotFound)
    .WithName("GetFornecedorPorId")
    .WithTags("Fornecedor");

app.MapPost("/fornecedor", async (
    MinimalContextDb context,
    Fornecedor fornecedor) =>
{
    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Add(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
        ? Results.CreatedAtRoute("GetFornecedorPorId", new { id = fornecedor.Id })
        : Results.BadRequest("Houve um problema ao salvar o registro");

}).ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PostFornecedor")
    .WithTags("Fornecedor");

app.MapPut("/fornecedor/{id}", async (
    Guid id,
    MinimalContextDb context,
    Fornecedor fornecedor) =>
{
    var fornecedorBanco = await context.Fornecedores.AsNoTracking<Fornecedor>()
                                                    .FirstOrDefaultAsync(f => f.Id == id);
    if (fornecedorBanco != null) return Results.NotFound();

    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Update(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
        ? Results.NoContent()
        : Results.BadRequest("Houve um problema ao salvar o registro");

}).ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PutFornecedor")
    .WithTags("Fornecedor");


app.MapDelete("/fornecedor/{id}", async (
    Guid id,
    MinimalContextDb context) =>
{
    var fornecedor = await context.Fornecedores.FindAsync(id);
    if (fornecedor != null) return Results.NotFound();

    context.Fornecedores.Remove(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0
        ? Results.NoContent()
        : Results.BadRequest("Houve um problema ao salvar o registro");

}).ProducesValidationProblem()
    .Produces<Fornecedor>(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("DeleteFornecedor")
    .WithTags("Fornecedor");
app.Run();

