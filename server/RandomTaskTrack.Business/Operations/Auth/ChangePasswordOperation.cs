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

public class ChangePasswordOperation : BaseOperation<ChangePasswordRequest, ChangePasswordResponse>
{
    private readonly IUserRepository _userRepository;

    public ChangePasswordOperation(
        ILogger<ChangePasswordOperation> logger,
        IValidator<ChangePasswordRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IUserRepository userRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _userRepository = userRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<ChangePasswordResponse> Execute(ChangePasswordRequest request, IUnitOfWork unitOfWork)
    {
        User user = await _userRepository.GetByIdAsync(request.SessionUserData.Id, unitOfWork)
                    ?? throw new NotFoundException("User not found", ExceptionCodes.USER_UNAUTHORIZED);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
        {
            throw new BadRequestException("Current password is incorrect", ExceptionCodes.AUTH_CURRENT_PASSWORD_INVALID);
        }

        string hashed = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, AuthConstants.PasswordWorkFactor);
        await _userRepository.UpdatePasswordAsync(user.Id, hashed, unitOfWork);

        return new ChangePasswordResponse { Success = true };
    }
}
