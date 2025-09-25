using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbapp.LocalData
{
    public class Setting
    {
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }
		public string FBApiURL { get; set; } = "http://streetsmart-001-site9.gtempurl.com";
		public string HRISApiURL { get; set; } = "http://streetsmart-001-site10.otempurl.com";
		public string CurrenBioId { get; set; } = "1";
		public string CurrentPassword { get; set; } = "1234";
		public string Camera { get; set; } = "front";
		public int ImageRotate { get; set; } = 270;
		public int DoublePunchInterval { get; set; } = 120;
        public bool BypassRestriction { get; set; }
    }
}
