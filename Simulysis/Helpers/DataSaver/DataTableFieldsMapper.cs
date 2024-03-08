using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulysis.Helpers.DataSaver
{
    public class DataTableFieldsMapper
    {
        // just for testing
        Dictionary<string, int> fields;
        public DataTableFieldsMapper(DataTable dataTable)
        {
            fields = new Dictionary<string, int>();
            int count = 0;
            foreach (DataColumn column in dataTable.Columns)
            {
                    fields.Add(column.ColumnName.ToString().ToLower(), count);
                    count++;
                    continue;
            }
        }
        public int getOrderOf(string destinationField)
        {
            int order = fields[destinationField.ToLower()];
            return order;
        }
    }
}
