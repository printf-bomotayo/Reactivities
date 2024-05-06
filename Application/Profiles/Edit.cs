using Application.Core;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Profiles
{
    public class Edit
    {
        public class Command : IRequest<Result<Unit>>
        {
            public string DisplayName { get; set; }
            public string Bio { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.DisplayName).NotEmpty();  
            }
        }

        public class Handler : IRequestHandler<Command, Result<Unit>>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;
            private readonly IUserAccessor _userAccessor;
            public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _context = context;
                _mapper = mapper;

            }


            public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _context.Users.FirstOrDefaultAsync(x =>x.UserName == _userAccessor.GetUsername());

                if(user == null) return Result<Unit>.Failure("User not found");;

                // Store the initial state of user for comparison
                var initialUser = new AppUser
                {
                    Bio = user.Bio,
                    DisplayName = user.DisplayName                    
                };

                // Update user properties if request contains non-null values
                user.Bio = request.Bio ?? user.Bio;
                user.DisplayName = request.DisplayName ?? user.DisplayName;

                // Check if any changes were made to the user object
                bool hasChanges = user.Bio != initialUser.Bio || user.DisplayName != initialUser.DisplayName;

                // If there are no changes, return success
                if (!hasChanges)
                    return Result<Unit>.Failure("No changes observed");

                // Save changes to the database
                var success = await _context.SaveChangesAsync() > 0;

                if (success)
                    return Result<Unit>.Success(Unit.Value);
                else
                    return Result<Unit>.Failure("Problem updating profile");       
            }

        }
    }
}