using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Auth;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Auth;
using RandomTaskTrack.Data.Response.Auth;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Auth;

namespace RandomTaskTrack.Business.Operations.Auth;

public class RegisterOperation : BaseOperation<RegisterRequest, RegisterResponse>
{
    private readonly IUserRepository _userRepository;

    public RegisterOperation(
        ILogger<RegisterOperation> logger,
        IValidator<RegisterRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IUserRepository userRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _userRepository = userRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<RegisterResponse> Execute(RegisterRequest request, IUnitOfWork unitOfWork)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, unitOfWork))
        {
            throw new BadRequestException("Email already registered", ExceptionCodes.AUTH_EMAIL_ALREADY_EXISTS);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password, AuthConstants.PasswordWorkFactor),
            Role = request.Role
        };

        await _userRepository.CreateAsync(user, unitOfWork);

        return new RegisterResponse { UserId = user.Id };
    }
}
