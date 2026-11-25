using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;

namespace Application.JsonPlaceholder.Commands.CreatePost;

internal sealed class CreatePostCommandHandler(IJsonPlaceholderClient jsonPlaceholder)
    : ICommandHandler<CreatePostCommand, Result<JsonPlaceholderPostResponse>>
{
    public Task<Result<JsonPlaceholderPostResponse>> HandleAsync(
        CreatePostCommand command, CancellationToken cancellationToken) =>
        jsonPlaceholder.CreatePostAsync(command.UserId, command.Title, command.Body, cancellationToken);
}
