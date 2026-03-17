namespace LearningTracker.Api.API.Models;

public class ResultData<T>
{
    public bool Success { get; set; }

    public string Message { get; set; }

    public T Value { get; set; }
}

