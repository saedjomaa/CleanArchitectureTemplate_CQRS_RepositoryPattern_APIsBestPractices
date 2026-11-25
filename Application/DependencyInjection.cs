using Application.Common.Abstractions;
using Application.Common.Decorators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.Scan(scan => scan.FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes
                .AssignableToAny(typeof(ICommandHandler<,>), typeof(IQueryHandler<,>))
                .Where(type => !type.IsGenericTypeDefinition), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)), publicOnly: false)
            .As(type => type.GetInterfaces().Where(service =>
                service.IsGenericType && service.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .WithScopedLifetime());

        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.Command<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationDecorator.Query<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.Command<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.Query<,>));

        return services;
    }
}
