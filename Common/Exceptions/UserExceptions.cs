namespace sgspe.Common.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string message) : base(message) { }
}

public class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string message, Microsoft.EntityFrameworkCore.DbUpdateException ex) : base(message) { }
}

public class DuplicateNameException : Exception
{
    public DuplicateNameException(string message) : base(message) { }
}

public class InvalidUserOperationException : Exception
{
    public InvalidUserOperationException(string message) : base(message) { }
}