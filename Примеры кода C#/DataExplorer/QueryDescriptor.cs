using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitel.SCADA.Tools.DataExplorer.Models
{
    public class QueryDescriptor : IQueryDescriptor
    {
        public QyeryTypes QueryItemType { get; set; }

        public DateTime DateQueryS { get; set; }

        public DateTime DateQueryPo { get; set; }

        public StepMeasureTypes StepType { get; set; }

        public QueryTimeTypes QueryTimeType { get; set; }

        public byte WatchStep { get; set; }

        public string QueryName { get; set; }

        public string[] QueryItems { get; set; }
    }
}
