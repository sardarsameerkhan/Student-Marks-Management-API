namespace MarksApi.Services;

public abstract class ApiException(string message) : Exception(message);
public sealed class ApiNotFoundException(string message) : ApiException(message);
public sealed class ApiConflictException(string message) : ApiException(message);
public sealed class ApiForbiddenException(string message) : ApiException(message);
public sealed class ApiUnauthorizedException(string message) : ApiException(message);
