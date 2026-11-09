using Microsoft.Extensions.DependencyInjection;
using Wortlet.Application.Auth;
using Wortlet.Application.Backup;
using Wortlet.Application.Folders;
using Wortlet.Application.Groups;
using Wortlet.Application.Languages;
using Wortlet.Application.Onboarding;
using Wortlet.Application.Profiles;
using Wortlet.Application.Quiz;
using Wortlet.Application.Recognition;
using Wortlet.Application.Sets;
using Wortlet.Application.Statistics;

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
        services.AddScoped<ProfileService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<SetService>();
        services.AddScoped<GroupService>();
        return services;
    }
}
