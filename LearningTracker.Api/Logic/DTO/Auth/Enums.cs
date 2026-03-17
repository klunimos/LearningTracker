namespace LearningTracker.Api.Logic.DTO.Auth;

public enum RegisterStatus { Success, EmailAlreadyExists }

public enum LoginStatus { Success, InvalidCredentials }

public enum GoogleLoginStatus { Success, InvalidToken }
