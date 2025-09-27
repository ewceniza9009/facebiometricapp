using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace fbapp.LocalData
{
	public class DbContext
	{
		private SQLiteAsyncConnection _database;

		public DbContext(string dbPath)
		{
			_database = new SQLiteAsyncConnection(dbPath);
			_database.CreateTableAsync<Setting>().Wait();
			_database.CreateTableAsync<DTRLog>().Wait();
		}

		// CRUD operations for Setting
		public Task<int> SaveSettingAsync(Setting setting)
		{
			if (setting.Id != 0)
			{
				return _database.UpdateAsync(setting);
			}
			else
			{
				return _database.InsertAsync(setting);
			}
		}

		public Task<List<Setting>> GetSettingsAsync()
		{
			return _database.Table<Setting>().ToListAsync();
		}

		public async Task<Setting> GetSettingFirstAsync()
		{
			if (await _database.Table<Setting>().CountAsync() == 0) 
			{
				var newSetting = new Setting();
				await SaveSettingAsync(newSetting);
			}

			return await _database.Table<Setting>().FirstOrDefaultAsync();
		}

		public Task<Setting> GetSettingByIdAsync(int id)
		{
			return _database.Table<Setting>().Where(i => i.Id == id).FirstOrDefaultAsync();
		}

		public Task<int> DeleteSettingAsync(Setting setting)
		{
			return _database.DeleteAsync(setting);
		}

		// CRUD operations for DTRLog
		public Task<int> SaveDTRLogAsync(DTRLog log)
		{
			if (log.Id != 0)
			{
				return _database.UpdateAsync(log);
			}
			else
			{
				return _database.InsertAsync(log);
			}
		}

		public Task<List<DTRLog>> GetDTRLogsAsync()
		{
			return _database.Table<DTRLog>().ToListAsync();
		}

		public async Task<List<DTRLog>> GetDTRLogsQueryAsync(DateTime startDate, DateTime endDate, string? name = "")
		{
			var logs = await GetDTRLogsAsync();

			if (!string.IsNullOrEmpty(name))
			{
				return logs
					.Where(x => x.Log.Date >= startDate.Date &&
								x.Log.Date <= endDate.Date &&
								x.Name.ToLower().Contains(name.ToLower()))
                    .OrderByDescending(x => x.Log)
                    .ToList();
			}

			var result = logs
				.Where(x => x.Log.Date >= startDate.Date &&
							x.Log.Date <= endDate.Date)
                .OrderByDescending(x => x.Log)
                .ToList();

			return result;
		}

		public Task<DTRLog> GetDTRLogByIdAsync(int id)
		{
			return _database.Table<DTRLog>().Where(i => i.Id == id).FirstOrDefaultAsync();
		}

        public Task<DTRLog> GetDTRLogByBioIdAsync(string bioId, string logType)
        {
            return _database.Table<DTRLog>()
				.Where(i => i.BioId == bioId && i.LogType == logType)
				.FirstOrDefaultAsync();
        }

        public Task<int> DeleteDTRLogAsync(DTRLog log)
		{
			return _database.DeleteAsync(log);
		}

        public Task<int> DeleteDTRLogsByBioIdAsync(string bioId)
        {
            return _database.Table<DTRLog>()
				.Where(i => i.BioId == bioId && i.Name != "OFFLINE")
				.DeleteAsync();
        }

        public Task<int> DeleteDTROfflineLogsByBioIdAsync()
        {
            return _database.Table<DTRLog>()
                .Where(i => i.Name == "OFFLINE")
                .DeleteAsync();
        }

        public async Task<int> CleanUpOfflineLogsAsync()
        {
            var logs = await _database.Table<DTRLog>()
                .Where(i => i.Name.Contains("OFFLINE"))
                .ToListAsync();

            foreach (var log in logs)
            {
                // remove the "OFFLINE" part from the name
                log.Name = log.Name.Replace("OFFLINE", "");
                await _database.UpdateAsync(log);
            }

            return logs.Count;
        }

    }
}
