using Microsoft.Extensions.DependencyInjection;
using Wortlet.Application.Auth;
using Wortlet.Application.Backup;
using Wortlet.Application.Folders;
using Wortlet.Application.Languages;
using Wortlet.Application.Onboarding;
using Wortlet.Application.Quiz;
using Wortlet.Application.Recognition;

namespace Wortlet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<LanguageService>();
        services.AddScoped<FolderService>();
        services.AddScoped<QuizService>();
        services.AddScoped<BackupService>();
        services.AddScoped<OnboardingService>();
        services.AddScoped<RecognitionService>();
        return services;
    }
}
