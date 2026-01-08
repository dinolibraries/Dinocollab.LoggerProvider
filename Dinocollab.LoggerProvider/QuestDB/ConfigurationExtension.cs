using Dinocollab.LoggerProvider.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dinocollab.LoggerProvider.QuestDB
{
    public static  class ConfigurationExtension
    {
        public static IServiceCollection AddQuestDBLoggerProvider(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton(typeof(QuestDbLogWorker<HttpContextMessageLog>));
            services.AddHostedService(provider => provider.GetRequiredService<QuestDbLogWorker<HttpContextMessageLog>>());
            services.AddSingleton<HttpContextExtractLog>();
            return services;
        }
        public static IServiceCollection AddQuestDBLoggerProvider(this IServiceCollection services, Action<QuestDBLoggerOption> configure)
        {
            services.Configure(configure);
            return services.AddQuestDBLoggerProvider();
        }
        public static IServiceCollection AddQuestDBLoggerProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<QuestDBLoggerOption>(configuration.GetSection(nameof(QuestDBLoggerOption)));
            return services.AddQuestDBLoggerProvider();
        }
        public static WebApplication UseQuestDBLoggerProvider(this WebApplication app)
        {
            HttpContextExtractLog httpContextExtractLog = app.Services.GetRequiredService<HttpContextExtractLog>();
            // This will start the background service if not already started
            app.Use(async (context, next) =>
            {
                try
                {
                    await httpContextExtractLog.LogAsync(next);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await httpContextExtractLog.LogAsync(next: null, error: ex);
                    throw;
                }
            });
            return app;
        }
    }
}
