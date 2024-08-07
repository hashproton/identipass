﻿using Application.UseCases.RefreshTokens.Comands;

namespace Application.UseCases.Auth.Commands;

public record LoginUserCommand(
    string? UsernameOrEmail,
    string Password) : IRequest<Result<LoginUserCommandResponse>>;

public class LoginUserCommandHandler(
    ILogger logger,
    IUsersRepository usersRepository,
    IPasswordService passwordService,
    ITokenService tokenService,
    ISender mediator) : IRequestHandler<LoginUserCommand, Result<LoginUserCommandResponse>>
{
    public async Task<Result<LoginUserCommandResponse>> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.UsernameOrEmail))
        {
            return Result.Failure<LoginUserCommandResponse>(UserErrors.UsernameOrEmailRequired);
        }
        
        var isEmail = request.UsernameOrEmail.Contains("@");
        
        var user = isEmail
            ? await usersRepository.GetUserByEmail(request.UsernameOrEmail, cancellationToken)
            : await usersRepository.GetUserByUsername(request.UsernameOrEmail, cancellationToken);

        if (user is null)
        {
            return Result.Failure<LoginUserCommandResponse>(UserErrors.InvalidCredentials);
        }

        if (!passwordService.VerifyPassword(request.Password, user.Password))
        {
            return Result.Failure<LoginUserCommandResponse>(UserErrors.InvalidCredentials);
        }

        logger.LogInformation($"[User] {user.Id} logged in");

        var token = tokenService.GenerateToken(user);
        var refreshToken = await mediator.Send(new CreateRefreshTokenCommand(user.Id), cancellationToken);

        return Result.Success(new LoginUserCommandResponse(token, refreshToken.Value));
    }
}

public record LoginUserCommandResponse(
    string Token,
    string RefreshToken);