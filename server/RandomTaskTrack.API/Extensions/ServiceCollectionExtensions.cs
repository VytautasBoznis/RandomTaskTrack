using FluentValidation;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Business.Ai;
using RandomTaskTrack.Business.Ai.Providers;
using RandomTaskTrack.Business.Auth;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Auth;
using RandomTaskTrack.Business.Operations.Chat;
using RandomTaskTrack.Business.Operations.Domains;
using RandomTaskTrack.Business.Operations.Finance;
using RandomTaskTrack.Business.Operations.Notes;
using RandomTaskTrack.Business.Operations.Plants;
using RandomTaskTrack.Business.Operations.Recipes;
using RandomTaskTrack.Business.Operations.Recurrences;
using RandomTaskTrack.Business.Operations.Tasks;
using RandomTaskTrack.Business.Finance.Sources;
using RandomTaskTrack.Business.Plants;
using RandomTaskTrack.Business.Recipes;
using RandomTaskTrack.Business.Recipes.Sources;
using RandomTaskTrack.Business.Repositories.Auth;
using RandomTaskTrack.Business.Repositories.Chat;
using RandomTaskTrack.Business.Repositories.Domains;
using RandomTaskTrack.Business.Repositories.Finance;
using RandomTaskTrack.Business.Repositories.Notes;
using RandomTaskTrack.Business.Repositories.Plants;
using RandomTaskTrack.Business.Repositories.Recipes;
using RandomTaskTrack.Business.Repositories.Recurrences;
using RandomTaskTrack.Business.Repositories.Tasks;
using RandomTaskTrack.Business.Services;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Request.Auth;
using RandomTaskTrack.Data.Request.Chat;
using RandomTaskTrack.Data.Request.Domains;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Request.Notes;
using RandomTaskTrack.Data.Request.Plants;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Request.Recurrences;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Validator.Auth;
using RandomTaskTrack.Data.Validator.Chat;
using RandomTaskTrack.Data.Validator.Domains;
using RandomTaskTrack.Data.Validator.Finance;
using RandomTaskTrack.Data.Validator.Notes;
using RandomTaskTrack.Data.Validator.Plants;
using RandomTaskTrack.Data.Validator.Recipes;
using RandomTaskTrack.Data.Validator.Recurrences;
using RandomTaskTrack.Data.Validator.Tasks;
using RandomTaskTrack.Interfaces.Ai;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Finance;
using RandomTaskTrack.Interfaces.Plants;
using RandomTaskTrack.Interfaces.Recipes;
using RandomTaskTrack.Interfaces.Repositories.Auth;
using RandomTaskTrack.Interfaces.Repositories.Chat;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Finance;
using RandomTaskTrack.Interfaces.Repositories.Notes;
using RandomTaskTrack.Interfaces.Repositories.Plants;
using RandomTaskTrack.Interfaces.Repositories.Recipes;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfig(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection(AppSettingKeys.JwtSection));
        services.Configure<DatabaseOptions>(config.GetSection(AppSettingKeys.DatabaseSection));
        services.Configure<AiOptions>(config.GetSection(AppSettingKeys.AiSection));
        services.Configure<RecipeOptions>(config.GetSection(AppSettingKeys.RecipesSection));
        services.Configure<FinanceOptions>(config.GetSection(AppSettingKeys.FinanceSection));
        services.Configure<SchedulerOptions>(config.GetSection(AppSettingKeys.SchedulerSection));

        return services;
    }

    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();
        services.AddSingleton<JwtService>();
        services.AddSingleton<IClock, Clock>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDomainsRepository, DomainsRepository>();
        services.AddScoped<ITasksRepository, TasksRepository>();
        services.AddScoped<ICompletionsRepository, CompletionsRepository>();
        services.AddScoped<IRecurrencesRepository, RecurrencesRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IRecipesRepository, RecipesRepository>();
        services.AddScoped<INotesRepository, NotesRepository>();
        services.AddScoped<IFinanceRepository, FinanceRepository>();
        services.AddScoped<IPlantsRepository, PlantsRepository>();

        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IRecurrenceMaterializer, RecurrenceMaterializer>();
        services.AddHostedService<RecurrenceMaterializerHostedService>();
        services.AddScoped<IRecipePicker, RecipePicker>();
        services.AddScoped<IFinanceProjector, FinanceProjector>();

        // No key check and no null implementation: IAiProvider already has one,
        // so an unconfigured app fails the lookup with a clear message and adds
        // the plant anyway.
        services.AddScoped<IPlantResearcher, AiPlantResearcher>();

        return services;
    }

    /// <summary>
    /// The price source. Same shape as AddRecipeServices, minus the key check:
    /// Yahoo needs no account, so the default path has nothing to configure and
    /// nothing to forget.
    /// </summary>
    public static IServiceCollection AddFinanceServices(this IServiceCollection services)
    {
        services.AddHttpClient(PriceSourceNames.Yahoo);

        services.AddSingleton<IStockPriceSource>(sp =>
        {
            FinanceOptions options = sp.GetRequiredService<IOptions<FinanceOptions>>().Value;

            return options.Provider?.ToLowerInvariant() switch
            {
                PriceSourceNames.Yahoo or null or "" => new YahooPriceSource(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<IOptions<FinanceOptions>>(),
                    sp.GetRequiredService<ILogger<YahooPriceSource>>()),
                PriceSourceNames.Null => new NullPriceSource(),
                _ => throw new InvalidOperationException(
                    $"Unknown price source '{options.Provider}'. Supported: {PriceSourceNames.Yahoo}, {PriceSourceNames.Null}.")
            };
        });

        return services;
    }

    /// <summary>
    /// Resolves the configured recipe source. Same shape as AddAiServices: no
    /// key means a null source, so the app still boots and only the recipes tab
    /// reports why.
    /// </summary>
    public static IServiceCollection AddRecipeServices(this IServiceCollection services)
    {
        services.AddHttpClient(RecipeSourceNames.Spoonacular);
        services.AddHttpClient(nameof(RecipeCatalogImporter));

        services.AddSingleton<IRecipeCatalogImporter, RecipeCatalogImporter>();

        // The rotation and search want different things, so the registered
        // source is a pair: an API for "pick me a Thai dish" (cuisine labels,
        // images, timings) and the local corpus for "I want ramen" (breadth).
        // See HybridRecipeSource.
        services.AddSingleton<IRecipeSource>(sp =>
        {
            RecipeOptions options = sp.GetRequiredService<IOptions<RecipeOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<Program>>();

            IRecipeSource rotation = BuildRotationSource(sp, options, logger);
            var catalog = new CatalogRecipeSource(sp.GetRequiredService<IUnitOfWorkFactory>());

            return new HybridRecipeSource(rotation, catalog, sp.GetRequiredService<ILogger<HybridRecipeSource>>());
        });

        return services;
    }

    private static IRecipeSource BuildRotationSource(IServiceProvider sp, RecipeOptions options, ILogger logger)
    {
        // Still only a warning with no key: the catalog needs none, so search
        // and the whole cookbook keep working and only the weekly pick reports
        // why it cannot roll.
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            logger.LogWarning("No recipe API key configured — the weekly rotation is disabled. Set Recipes__ApiKey to enable it.");

            return new NullRecipeSource();
        }

        return options.Provider?.ToLowerInvariant() switch
        {
            RecipeSourceNames.Spoonacular or null or "" => new SpoonacularRecipeSource(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IOptions<RecipeOptions>>(),
                sp.GetRequiredService<ILogger<SpoonacularRecipeSource>>()),
            RecipeSourceNames.Null => new NullRecipeSource(),
            _ => throw new InvalidOperationException(
                $"Unknown recipe source '{options.Provider}'. Supported: {RecipeSourceNames.Spoonacular}, {RecipeSourceNames.Null}.")
        };
    }

    /// <summary>
    /// Resolves the configured AI provider. Everything above IAiProvider is
    /// provider-agnostic, so adding another one means adding a case here and a
    /// class in Ai/Providers — nothing else changes.
    /// </summary>
    public static IServiceCollection AddAiServices(this IServiceCollection services)
    {
        services.AddScoped<IAiToolRegistry, AiToolRegistry>();
        services.AddScoped<IAiConversationService, AiConversationService>();

        services.AddSingleton<IAiProvider>(sp =>
        {
            AiOptions options = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<Program>>();

            // Falling back to the null provider rather than throwing keeps the
            // whole app usable without an API key — only chat degrades.
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                logger.LogWarning("No AI API key configured — chat is disabled. Set Ai__ApiKey to enable it.");
                return new NullAiProvider();
            }

            return options.Provider?.ToLowerInvariant() switch
            {
                AiProviderNames.Anthropic or null or "" => new AnthropicAiProvider(
                    sp.GetRequiredService<IOptions<AiOptions>>(),
                    sp.GetRequiredService<ILogger<AnthropicAiProvider>>()),
                AiProviderNames.Null => new NullAiProvider(),
                _ => throw new InvalidOperationException(
                    $"Unknown AI provider '{options.Provider}'. Supported: {AiProviderNames.Anthropic}, {AiProviderNames.Null}.")
            };
        });

        return services;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        // Auth
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddSingleton<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddSingleton<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();

        // Domains
        services.AddSingleton<IValidator<GetDomainsRequest>, GetDomainsRequestValidator>();

        // Tasks
        services.AddSingleton<IValidator<GetDashboardRequest>, GetDashboardRequestValidator>();
        services.AddSingleton<IValidator<GetTasksRequest>, GetTasksRequestValidator>();
        services.AddSingleton<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
        services.AddSingleton<IValidator<UpdateTaskRequest>, UpdateTaskRequestValidator>();
        services.AddSingleton<IValidator<CompleteTaskRequest>, CompleteTaskRequestValidator>();
        services.AddSingleton<IValidator<DeleteTaskRequest>, DeleteTaskRequestValidator>();
        services.AddSingleton<IValidator<GetCompletionLogRequest>, GetCompletionLogRequestValidator>();

        // Recurrences
        services.AddSingleton<IValidator<GetRecurrencesRequest>, GetRecurrencesRequestValidator>();
        services.AddSingleton<IValidator<CreateRecurrenceRequest>, CreateRecurrenceRequestValidator>();
        services.AddSingleton<IValidator<UpdateRecurrenceRequest>, UpdateRecurrenceRequestValidator>();
        services.AddSingleton<IValidator<DeleteRecurrenceRequest>, DeleteRecurrenceRequestValidator>();

        // Recipes
        services.AddSingleton<IValidator<GetWeeklyDishRequest>, GetWeeklyDishRequestValidator>();
        services.AddSingleton<IValidator<RerollDishRequest>, RerollDishRequestValidator>();
        services.AddSingleton<IValidator<CreateDishTaskRequest>, CreateDishTaskRequestValidator>();
        services.AddSingleton<IValidator<SearchRecipesRequest>, SearchRecipesRequestValidator>();
        services.AddSingleton<IValidator<SaveRecipesRequest>, SaveRecipesRequestValidator>();
        services.AddSingleton<IValidator<SetWeeklyDishRequest>, SetWeeklyDishRequestValidator>();
        services.AddSingleton<IValidator<UpdateRecipeRequest>, UpdateRecipeRequestValidator>();
        services.AddSingleton<IValidator<GetRecipeHistoryRequest>, GetRecipeHistoryRequestValidator>();
        services.AddSingleton<IValidator<GetCatalogStatusRequest>, GetCatalogStatusRequestValidator>();
        services.AddSingleton<IValidator<StartCatalogImportRequest>, StartCatalogImportRequestValidator>();
        services.AddSingleton<IValidator<ClearWeeklyDishRequest>, ClearWeeklyDishRequestValidator>();

        // Notes
        services.AddSingleton<IValidator<GetNotesRequest>, GetNotesRequestValidator>();
        services.AddSingleton<IValidator<CreateNoteRequest>, CreateNoteRequestValidator>();
        services.AddSingleton<IValidator<UpdateNoteRequest>, UpdateNoteRequestValidator>();
        services.AddSingleton<IValidator<DeleteNoteRequest>, DeleteNoteRequestValidator>();

        // Finance
        services.AddSingleton<IValidator<GetFinanceOverviewRequest>, GetFinanceOverviewRequestValidator>();
        services.AddSingleton<IValidator<CreateAccountRequest>, CreateAccountRequestValidator>();
        services.AddSingleton<IValidator<UpdateAccountRequest>, UpdateAccountRequestValidator>();
        services.AddSingleton<IValidator<DeleteAccountRequest>, DeleteAccountRequestValidator>();
        services.AddSingleton<IValidator<SetAccountBalanceRequest>, SetAccountBalanceRequestValidator>();
        services.AddSingleton<IValidator<GetProjectionRequest>, GetProjectionRequestValidator>();
        services.AddSingleton<IValidator<RefreshPricesRequest>, RefreshPricesRequestValidator>();
        services.AddSingleton<IValidator<CreateFlowRequest>, CreateFlowRequestValidator>();
        services.AddSingleton<IValidator<UpdateFlowRequest>, UpdateFlowRequestValidator>();
        services.AddSingleton<IValidator<DeleteFlowRequest>, DeleteFlowRequestValidator>();
        services.AddSingleton<IValidator<GetEntriesRequest>, GetEntriesRequestValidator>();
        services.AddSingleton<IValidator<CreateEntryRequest>, CreateEntryRequestValidator>();
        services.AddSingleton<IValidator<UpdateEntryRequest>, UpdateEntryRequestValidator>();
        services.AddSingleton<IValidator<DeleteEntryRequest>, DeleteEntryRequestValidator>();
        services.AddSingleton<IValidator<CreateHoldingRequest>, CreateHoldingRequestValidator>();
        services.AddSingleton<IValidator<UpdateHoldingRequest>, UpdateHoldingRequestValidator>();
        services.AddSingleton<IValidator<DeleteHoldingRequest>, DeleteHoldingRequestValidator>();
        services.AddSingleton<IValidator<CreateTradeRequest>, CreateTradeRequestValidator>();
        services.AddSingleton<IValidator<UpdateTradeRequest>, UpdateTradeRequestValidator>();
        services.AddSingleton<IValidator<DeleteTradeRequest>, DeleteTradeRequestValidator>();
        services.AddSingleton<IValidator<CreateDividendRequest>, CreateDividendRequestValidator>();
        services.AddSingleton<IValidator<UpdateDividendRequest>, UpdateDividendRequestValidator>();
        services.AddSingleton<IValidator<DeleteDividendRequest>, DeleteDividendRequestValidator>();
        services.AddSingleton<IValidator<CreateDepositRequest>, CreateDepositRequestValidator>();
        services.AddSingleton<IValidator<UpdateDepositRequest>, UpdateDepositRequestValidator>();
        services.AddSingleton<IValidator<DeleteDepositRequest>, DeleteDepositRequestValidator>();
        services.AddSingleton<IValidator<CreateTargetRequest>, CreateTargetRequestValidator>();
        services.AddSingleton<IValidator<UpdateTargetRequest>, UpdateTargetRequestValidator>();
        services.AddSingleton<IValidator<DeleteTargetRequest>, DeleteTargetRequestValidator>();

        // Plants
        services.AddSingleton<IValidator<GetPlantsRequest>, GetPlantsRequestValidator>();
        services.AddSingleton<IValidator<CreatePlantRequest>, CreatePlantRequestValidator>();
        services.AddSingleton<IValidator<UpdatePlantRequest>, UpdatePlantRequestValidator>();
        services.AddSingleton<IValidator<DeletePlantRequest>, DeletePlantRequestValidator>();
        services.AddSingleton<IValidator<ResearchPlantRequest>, ResearchPlantRequestValidator>();
        services.AddSingleton<IValidator<CreatePlantScheduleRequest>, CreatePlantScheduleRequestValidator>();
        services.AddSingleton<IValidator<AddPlantPhotoRequest>, AddPlantPhotoRequestValidator>();
        services.AddSingleton<IValidator<GetPlantPhotoRequest>, GetPlantPhotoRequestValidator>();
        services.AddSingleton<IValidator<DeletePlantPhotoRequest>, DeletePlantPhotoRequestValidator>();
        services.AddSingleton<IValidator<CreateSowingPlanRequest>, CreateSowingPlanRequestValidator>();

        // Chat
        services.AddSingleton<IValidator<SendChatMessageRequest>, SendChatMessageRequestValidator>();
        services.AddSingleton<IValidator<GetConversationsRequest>, GetConversationsRequestValidator>();
        services.AddSingleton<IValidator<GetConversationRequest>, GetConversationRequestValidator>();
        services.AddSingleton<IValidator<DeleteConversationRequest>, DeleteConversationRequestValidator>();

        return services;
    }

    public static IServiceCollection AddOperations(this IServiceCollection services)
    {
        services.AddScoped<OperationFactory>();

        // Auth
        services.AddScoped<LoginOperation>();
        services.AddScoped<RegisterOperation>();
        services.AddScoped<ChangePasswordOperation>();

        // Domains
        services.AddScoped<GetDomainsOperation>();

        // Tasks
        services.AddScoped<GetDashboardOperation>();
        services.AddScoped<GetTasksOperation>();
        services.AddScoped<CreateTaskOperation>();
        services.AddScoped<UpdateTaskOperation>();
        services.AddScoped<CompleteTaskOperation>();
        services.AddScoped<DeleteTaskOperation>();
        services.AddScoped<GetCompletionLogOperation>();

        // Recurrences
        services.AddScoped<GetRecurrencesOperation>();
        services.AddScoped<CreateRecurrenceOperation>();
        services.AddScoped<UpdateRecurrenceOperation>();
        services.AddScoped<DeleteRecurrenceOperation>();

        // Recipes
        services.AddScoped<GetWeeklyDishOperation>();
        services.AddScoped<RerollDishOperation>();
        services.AddScoped<CreateDishTaskOperation>();
        services.AddScoped<SearchRecipesOperation>();
        services.AddScoped<SaveRecipesOperation>();
        services.AddScoped<SetWeeklyDishOperation>();
        services.AddScoped<UpdateRecipeOperation>();
        services.AddScoped<GetRecipeHistoryOperation>();
        services.AddScoped<GetCatalogStatusOperation>();
        services.AddScoped<StartCatalogImportOperation>();
        services.AddScoped<ClearWeeklyDishOperation>();

        // Notes
        services.AddScoped<GetNotesOperation>();
        services.AddScoped<CreateNoteOperation>();
        services.AddScoped<UpdateNoteOperation>();
        services.AddScoped<DeleteNoteOperation>();

        // Finance
        services.AddScoped<GetFinanceOverviewOperation>();
        services.AddScoped<CreateAccountOperation>();
        services.AddScoped<UpdateAccountOperation>();
        services.AddScoped<DeleteAccountOperation>();
        services.AddScoped<SetAccountBalanceOperation>();
        services.AddScoped<GetProjectionOperation>();
        services.AddScoped<RefreshPricesOperation>();
        services.AddScoped<CreateFlowOperation>();
        services.AddScoped<UpdateFlowOperation>();
        services.AddScoped<DeleteFlowOperation>();
        services.AddScoped<GetEntriesOperation>();
        services.AddScoped<CreateEntryOperation>();
        services.AddScoped<UpdateEntryOperation>();
        services.AddScoped<DeleteEntryOperation>();
        services.AddScoped<CreateHoldingOperation>();
        services.AddScoped<UpdateHoldingOperation>();
        services.AddScoped<DeleteHoldingOperation>();
        services.AddScoped<CreateTradeOperation>();
        services.AddScoped<UpdateTradeOperation>();
        services.AddScoped<DeleteTradeOperation>();
        services.AddScoped<CreateDividendOperation>();
        services.AddScoped<UpdateDividendOperation>();
        services.AddScoped<DeleteDividendOperation>();
        services.AddScoped<CreateDepositOperation>();
        services.AddScoped<UpdateDepositOperation>();
        services.AddScoped<DeleteDepositOperation>();
        services.AddScoped<CreateTargetOperation>();
        services.AddScoped<UpdateTargetOperation>();
        services.AddScoped<DeleteTargetOperation>();

        // Plants
        services.AddScoped<GetPlantsOperation>();
        services.AddScoped<CreatePlantOperation>();
        services.AddScoped<UpdatePlantOperation>();
        services.AddScoped<DeletePlantOperation>();
        services.AddScoped<ResearchPlantOperation>();
        services.AddScoped<CreatePlantScheduleOperation>();
        services.AddScoped<AddPlantPhotoOperation>();
        services.AddScoped<GetPlantPhotoOperation>();
        services.AddScoped<DeletePlantPhotoOperation>();
        services.AddScoped<CreateSowingPlanOperation>();

        // Chat
        services.AddScoped<SendChatMessageOperation>();
        services.AddScoped<GetConversationsOperation>();
        services.AddScoped<GetConversationOperation>();
        services.AddScoped<DeleteConversationOperation>();

        return services;
    }
}
