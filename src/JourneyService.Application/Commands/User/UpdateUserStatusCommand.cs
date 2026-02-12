using JourneyService.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyService.Application.Commands.User
{
    public record UpdateUserStatusCommand(string UserId, UserStatus NewStatus, string AdminId) : IRequest;
}
