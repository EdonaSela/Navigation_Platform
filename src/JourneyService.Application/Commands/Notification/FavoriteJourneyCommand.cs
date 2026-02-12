using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Application.Commands.Notification
{
    public record FavoriteJourneyCommand(Guid JourneyId) : IRequest;
}
