using MongoDB.Driver;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Nupal.Core.Infrastructure.Repositories
{
    public class SystemSettingsRepository : ISystemSettingsRepository
    {
        private readonly IMongoCollection<SystemSettings> _settings;

        public SystemSettingsRepository(IConfiguration config)
        {
            var mongoUrl = config.GetValue<string>("MONGO_URL");
            var client = new MongoClient(mongoUrl);
            var db = client.GetDatabase("NupalDB");
            _settings = db.GetCollection<SystemSettings>("system_settings");
        }

        public async Task<SystemSettings> GetSettingsAsync()
        {
            var settings = await _settings.Find(s => s.Id == "Global").FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSettings();
                await _settings.InsertOneAsync(settings);
            }
            return settings;
        }

        public async Task UpdateSettingsAsync(SystemSettings settings)
        {
            await _settings.ReplaceOneAsync(s => s.Id == "Global", settings, new ReplaceOptions { IsUpsert = true });
        }
    }
}
