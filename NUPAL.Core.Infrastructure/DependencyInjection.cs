using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NUPAL.Core.Application.Interfaces;
using Nupal.Core.Infrastructure.Repositories;
using Nupal.Core.Infrastructure.Services;
using NUPAL.Core.Infrastructure.Services;

namespace NUPAL.Core.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var mongoUrl = configuration.GetValue<string>("MONGO_URL")
                           ?? Environment.GetEnvironmentVariable("MONGO_URL")
                           ?? throw new InvalidOperationException("MongoDB connection string is not configured. Please provide 'MONGO_URL' in appsettings or environment variables.");

            services.AddSingleton<IMongoClient>(_ =>
            {
                var settings = MongoClientSettings.FromConnectionString(mongoUrl);
                // Reduce timeouts to prevent long hangs during DNS/connectivity issues
                settings.ConnectTimeout = TimeSpan.FromSeconds(10);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
                return new MongoClient(settings);
            });
            services.AddSingleton<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return client.GetDatabase("nupal");
            });

            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IContactRepository, ContactRepository>();

            services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
            services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            services.AddHttpClient<IAgentClient, AgentClient>();

            services.AddScoped<IRlJobRepository, RlJobRepository>();
            services.AddScoped<IRlRecommendationRepository, RlRecommendationRepository>();
            services.AddHttpClient<IRlService, RlService>();
            services.AddScoped<IPrecomputeService, PrecomputeService>();

            // Register Wuzzuf job scraping service
            services.AddHttpClient<IJobService, WuzzufJobService>();
            
            services.AddScoped<IDynamicSkillsService, DynamicSkillsService>();
            
            // Register Resume Persistence
            services.AddScoped<IResumeRepository, ResumeRepository>();
            
            // Register Job Fit Analysis
            services.AddScoped<IJobFitService, JobFitService>();
            services.AddScoped<IJobFitRepository, JobFitRepository>();
            
            // Register Resume Parsing Services
            services.AddScoped<IPdfTextExtractorService, PdfTextExtractorService>();
            services.AddScoped<IResumeParsingService, GroqResumeParsingService>();

            // Register background worker for automatic sync
            services.AddHostedService<PrecomputeBackgroundWorker>();

            return services;
        }
    }
}
