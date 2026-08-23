using FluentValidation;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Business.Ai;
using RandomTaskTrack.Business.Ai.Providers;
using RandomTaskTrack.Business.Auth;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Business.Operations.Auth;
using RandomTaskTrack.Business.Operations.Chat;
using RandomTaskTrack.Business.Operations.Domains;
using RandomTaskTrack.Business.Operations.Recipes;
using RandomTaskTrack.Business.Operations.Recurrences;
using RandomTaskTrack.Business.Operations.Tasks;
using RandomTaskTrack.Business.Recipes.Sources;
using RandomTaskTrack.Business.Repositories.Auth;
using RandomTaskTrack.Business.Repositories.Chat;
using RandomTaskTrack.Business.Repositories.Domains;
using RandomTaskTrack.Business.Repositories.Recipes;
using RandomTaskTrack.Business.Repositories.Recurrences;
using RandomTaskTrack.Business.Repositories.Tasks;
using RandomTaskTrack.Business.Services;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Request.Auth;
using RandomTaskTrack.Data.Request.Chat;
using RandomTaskTrack.Data.Request.Domains;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Request.Recurrences;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Validator.Auth;
using RandomTaskTrack.Data.Validator.Chat;
using RandomTaskTrack.Data.Validator.Domains;
using RandomTaskTrack.Data.Validator.Recipes;
using RandomTaskTrack.Data.Validator.Recurrences;
using RandomTaskTrack.Data.Validator.Tasks;
using RandomTaskTrack.Interfaces.Ai;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Recipes;
using RandomTaskTrack.Interfaces.Repositories.Auth;
using RandomTaskTrack.Interfaces.Repositories.Chat;
using RandomTaskTrack.Interfaces.Repositories.Domains;
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

        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IRecurrenceMaterializer, RecurrenceMaterializer>();
        services.AddHostedService<RecurrenceMaterializerHostedService>();
        services.AddScoped<IRecipePicker, RecipePicker>();

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

        services.AddSingleton<IRecipeSource>(sp =>
        {
            RecipeOptions options = sp.GetRequiredService<IOptions<RecipeOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<Program>>();

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                logger.LogWarning("No recipe API key configured — the recipes tab is disabled. Set Recipes__ApiKey to enable it.");
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
        });

        return services;
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

        // Chat
        services.AddScoped<SendChatMessageOperation>();
        services.AddScoped<GetConversationsOperation>();
        services.AddScoped<GetConversationOperation>();
        services.AddScoped<DeleteConversationOperation>();

        return services;
    }
}
