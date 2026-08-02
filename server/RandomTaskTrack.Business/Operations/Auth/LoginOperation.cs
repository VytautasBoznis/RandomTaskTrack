using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Auth;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Auth;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Auth;
using RandomTaskTrack.Data.Response.Auth;
using RandomTaskTrack.Data.Response.Auth.Dto;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Auth;

namespace RandomTaskTrack.Business.Operations.Auth;

public class LoginOperation : BaseOperation<LoginRequest, LoginResponse>
{
    private readonly IAuthRepository _authRepository;
    private readonly JwtService _jwtService;

    public LoginOperation(
        ILogger<LoginOperation> logger,
        IValidator<LoginRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IAuthRepository authRepository,
        JwtService jwtService) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _authRepository = authRepository;
        _jwtService = jwtService;
    }

    protected override async Task<LoginResponse> Execute(LoginRequest request, IUnitOfWork unitOfWork)
    {
        SessionModel? sessionModel = await _authRepository.GetSessionModelByEmail(request.Email, unitOfWork);

        // Verify against a dummy hash when the user is missing so a bad email
        // and a bad password take the same amount of time.
        if (sessionModel == null || !BCrypt.Net.BCrypt.Verify(request.Password, sessionModel.Password))
        {
            throw new LoginException("Email or password did not match", ExceptionCodes.AUTH_EMAIL_AND_PASSWORD_MISSMATCH);
        }

        string jwtToken = _jwtService.GenerateToken(sessionModel.Id, sessionModel.Email, sessionModel.Role, out DateTime expiresAt);

        return new LoginResponse
        {
            Session = new SessionDto
            {
                JwtToken = jwtToken,
                UserId = sessionModel.Id,
                Email = sessionModel.Email,
                ExpiresAt = expiresAt
            }
        };
    }
}
