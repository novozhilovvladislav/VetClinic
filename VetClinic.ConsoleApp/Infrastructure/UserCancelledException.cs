namespace VetClinic.ConsoleApp.Infrastructure;

public sealed class UserCancelledException : Exception
{
    public UserCancelledException() : base("Операцію скасовано користувачем.")
    {
    }
}
