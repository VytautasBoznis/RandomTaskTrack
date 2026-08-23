using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Repositories.Recipes;
using RandomTaskTrack.Interfaces.Repositories.Tasks;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// Puts the dish on the board as an ordinary cooking task, so it shows up on
/// the dashboard and lands in the completion log like everything else.
/// </summary>
public class CreateDishTaskOperation : BaseOperation<CreateDishTaskRequest, CreateDishTaskResponse>
{
    private readonly IRecipesRepository _recipesRepository;
    private readonly ITasksRepository _tasksRepository;
    private readonly IDomainsRepository _domainsRepository;

    public CreateDishTaskOperation(
        ILogger<CreateDishTaskOperation> logger,
        IValidator<CreateDishTaskRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipesRepository recipesRepository,
        ITasksRepository tasksRepository,
        IDomainsRepository domainsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recipesRepository = recipesRepository;
        _tasksRepository = tasksRepository;
        _domainsRepository = domainsRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateDishTaskResponse> Execute(CreateDishTaskRequest request, IUnitOfWork unitOfWork)
    {
        RecipePick pick = await _recipesRepository.GetPickAsync(request.PickId, unitOfWork)
                          ?? throw new NotFoundException($"No dish pick with id {request.PickId}", ExceptionCodes.RECIPE_PICK_NOT_FOUND);

        Recipe recipe = await _recipesRepository.GetRecipeAsync(pick.RecipeId, unitOfWork)
                        ?? throw new NotFoundException("The picked dish is missing.", ExceptionCodes.RECIPE_PICK_NOT_FOUND);

        TaskDomain domain = await _domainsRepository.GetByCodeAsync(DomainCodes.Cooking, unitOfWork)
                            ?? throw new NotFoundException($"No '{DomainCodes.Cooking}' domain to file the dish under.", ExceptionCodes.DOMAIN_NOT_FOUND);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            DomainId = domain.Id,
            Title = recipe.Title,
            Notes = recipe.SourceUrl,

            // The payload is what makes the task traceable back to the dish —
            // the ingredients and method stay in recipe_recipes rather than
            // being copied into the task.
            Data = JsonSerializer.Serialize(new
            {
                recipeId = recipe.Id,
                pickId = pick.Id,
                sourceUrl = recipe.SourceUrl
            }),

            // Default to the last day of the dish's week: it is a "cook this
            // week" task, not a "cook today" one.
            DueOn = request.DueOn ?? pick.WeekOf.AddDays(6),
            Status = TaskItemStatus.Pending
        };

        await _tasksRepository.CreateAsync(task, unitOfWork);
        await _recipesRepository.SetPickTaskAsync(pick.Id, task.Id, unitOfWork);

        return new CreateDishTaskResponse
        {
            Task = await _tasksRepository.GetByIdAsync(task.Id, unitOfWork)
                   ?? throw new NotFoundException("Task not found after create", ExceptionCodes.TASK_NOT_FOUND)
        };
    }
}
