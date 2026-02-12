using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JourneyService.Domain.Events.JourneyEvents;

namespace JourneyService.Application.Commands.User
{
    public class UserStatusChangedEventHandler : INotificationHandler<UserStatusChangedEvent>
    {
        private readonly ILogger<UserStatusChangedEventHandler> _logger;

        public UserStatusChangedEventHandler(ILogger<UserStatusChangedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(UserStatusChangedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("User {UserId} status has been updated to {Status}. Publishing to message broker...",
                notification.UserId, notification.NewStatus);

            // TODO: Push this event to your Message Broker (RabbitMQ/Service Bus) 
            // so the Notification Service can send an email if needed.

            return Task.CompletedTask;
        }
    }
}
