using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;


[assembly: FshModule(typeof(FSH.Modules.Profile.ProfileModule), 601)]

namespace FSH.Modules.Profile;

public sealed class ProfileModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(ProfilePermissions.All);

        builder.Services.AddHeroDbContext<ProfileDbContext>();
        builder.Services.AddScoped<IDbInitializer, ProfileDbInitializer>();

        // OwnerType=Product policy for Files module attachments (product images).
        ////builder.Services.AddScoped<IFileAccessPolicy, ProductFileAccessPolicy>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ProfileDbContext>(
                name: "db:profile",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        // No custom middleware needed
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/profile")
            .WithTags("Profile")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Trash routes registered first so the literal `/trash` segment wins
        // over the catch-all `/{id:guid}`.
        //group.MapListTrashedBrandsEndpoint();
        //group.MapRestoreBrandEndpoint();
        //group.MapCreateBrandEndpoint();
        //group.MapUpdateBrandEndpoint();
        //group.MapDeleteBrandEndpoint();
        //group.MapGetBrandByIdEndpoint();
        //group.MapSearchBrandsEndpoint();

        // Category /tree and /trash must be registered before /{categoryId:guid}
        // so the literal routes win.
        //group.MapGetCategoryTreeEndpoint();
        //group.MapListTrashedCategoriesEndpoint();
        //group.MapRestoreCategoryEndpoint();
        //group.MapCreateCategoryEndpoint();
        //group.MapUpdateCategoryEndpoint();
        //group.MapDeleteCategoryEndpoint();
        //group.MapGetCategoryByIdEndpoint();
        //group.MapSearchCategoriesEndpoint();

        //group.MapListTrashedProductsEndpoint();
        //group.MapRestoreProductEndpoint();
        //group.MapCreateProductEndpoint();
        //group.MapUpdateProductEndpoint();
        //group.MapDeleteProductEndpoint();
        //group.MapChangeProductPriceEndpoint();
        //group.MapAdjustProductStockEndpoint();

        // Product images — collection sub-resource under /products/{id}/images.
        //group.MapAddProductImageEndpoint();
        //group.MapRemoveProductImageEndpoint();
        //group.MapSetProductThumbnailEndpoint();
        //group.MapReorderProductImagesEndpoint();

        //group.MapGetProductByIdEndpoint();
        //group.MapSearchProductsEndpoint();
    }
}
