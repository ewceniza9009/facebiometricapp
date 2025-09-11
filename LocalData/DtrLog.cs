using SQLite;

namespace fbapp.LocalData
{
    public class DTRLog
    {
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }
		public string BioId { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string LogType { get; set; } = string.Empty;		
		public DateTime Log { get; set; } = DateTime.Now;
	}
}
