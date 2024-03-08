using Entities.DTO;
using Entities.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulysis.Helpers.DataSaver.EntityDataTable
{
    public class LineDataTable :DTODataTable
    {
        public List<MySqlBulkCopyColumnMapping> ColumnMappings { get; set; }
        public DataTable lineDataTable { get; set; }

        public LineDataTable()
        {
            
        }

        public override List<MySqlBulkCopyColumnMapping> GetColumnMappings()
        {
            ColumnMappings = new List<MySqlBulkCopyColumnMapping>();
            DataTableFieldsMapper mapper = new DataTableFieldsMapper(this.lineDataTable);
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("Id"),
                DestinationColumn = "Id",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("FK_SystemId"),
                DestinationColumn = "FK_SystemId",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("FK_ProjectFileId"),
                DestinationColumn = "FK_ProjectFileId",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("Properties"),
                DestinationColumn = "Properties",
            });
            return ColumnMappings;
        }

        public  DataTable createDataTable(List<LineDTO> lineDTOs)
        {
            Loggers.SVP.Info("Create line data table");
            DataTable dataTable = new ListToDataTableConverter().ToDataTable(lineDTOs, "`line`");
            this.lineDataTable = dataTable;
            return dataTable;
        }
    }
}
