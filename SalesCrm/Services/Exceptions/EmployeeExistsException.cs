namespace SalesCrm.Services.Exceptions;

public class EmployeeExistsException : Exception
{
    public EmployeeExistsException(string message) : base(message)
    {}
}
