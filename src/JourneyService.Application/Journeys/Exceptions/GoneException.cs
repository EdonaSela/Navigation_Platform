namespace JourneyService.Application.Journey.Exceptions;

public class GoneException : Exception
{
    public GoneException(string message) : base(message) { }
}