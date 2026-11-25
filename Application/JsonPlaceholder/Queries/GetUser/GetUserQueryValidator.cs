using FluentValidation;

namespace Application.JsonPlaceholder.Queries.GetUser;

internal sealed class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(query => query.Id).GreaterThan(0);
    }
}
