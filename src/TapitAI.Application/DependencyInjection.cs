using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TapitAI.Application.Common.Behaviors;

namespace TapitAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxODA5NDc1MjAwIiwiaWF0IjoiMTc3ODAwMTYyOSIsImFjY291bnRfaWQiOiIwMTlkZjkyNzdmMTU3ODI4OGNmY2Y1ZjRkMGY5MTY0OSIsImN1c3RvbWVyX2lkIjoiY3RtXzAxa3F3amZtN3R0ZDduYTdoY2RzMWU2ajNjIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.WFp8W5-OBEpg43Ob-01Qx8M6tGCpp0l7dBORIG0VYPH9Seod3p-kl3PCUQZ_XOR6VJSnJSx4itqgLYax-6qwC4yjcP-Vperjps7m7G7sp7gnnENmSrcAZxR9AGmCUSQciPUjlnbvwLZkg1FrHJqq6qjx8ND_j09lhceZUEIWPFtZfv7dmish2KxcNS-JcSF02rdzOUVwlsnNmKQ-HoLq6nO9vR7xWyetHc-fD_UWfvkfr_WqK50gVcbR5CrMnVD-Qu2sF5R_aqqkdrN4IcxN5aEv_y9eRgNlWQ1JZirv8_KINtoHaUUi-AsQbtM6I3ox82_jWLuyXaejMj9SMR0-Qw";
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        return services;
    }
}
