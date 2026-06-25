namespace LearningTracker.Api.Logic.DTO.Auth;

public enum RegisterStatus { Success, EmailAlreadyExists }

public enum LoginStatus { Success, InvalidCredentials }

public enum GoogleLoginStatus { Success, InvalidToken }

public enum RefreshStatus { Success, InvalidToken, Expired, Revoked }

public enum ForgotPasswordStatus { Success }

public enum ResetPasswordStatus { Success, InvalidToken, Expired, Used }
