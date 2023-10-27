using RabbitMq.Connector.Model;

namespace RabbitMq.Connector.Tests;

public class FailureTest
{
    [Fact]
    public void WhenCreatingFailureShoudldSetPropertiesCorrectly()
    {
        var @event = new EventThatHasFailed();
        var exception = new Exception("Something wrong here...");
        var retryAttempt = 1;
        var failure = new Failure<EventThatHasFailed>(@event, retryAttempt, exception);
        
        failure.RetryAttempt.Should().Be(retryAttempt);
        @event.Should().Be(failure.Event);
        exception.Should().Be(failure.Exception);        
    }

    public class EventThatHasFailed : Event
    { }
}
