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
    public class ListDataTable :DTODataTable
    {
        public List<MySqlBulkCopyColumnMapping> ColumnMappings { get; set; }
        public DataTable listDataTable { get; set; }

        public ListDataTable()
        {
            

        }
        public override List<MySqlBulkCopyColumnMapping> GetColumnMappings()
        {
            DataTableFieldsMapper mapper = new DataTableFieldsMapper(this.listDataTable);
            ColumnMappings = new List<MySqlBulkCopyColumnMapping>();

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

        public  DataTable createDataTable(List<ListDTO> listDTOs)
        {
            Loggers.SVP.Info("Create list data table");
            DataTable dataTable = new ListToDataTableConverter().ToDataTable(listDTOs, "`list`");
            this.listDataTable = dataTable;
            return dataTable;
        }
    }
}
