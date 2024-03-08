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
    public class InstanceDataDataTable :DTODataTable
    {
        public List<MySqlBulkCopyColumnMapping> ColumnMappings { get; set; }
        public DataTable instanceDataDataTable { get; set; }

        public InstanceDataDataTable()
        {
            
        }
        public override List<MySqlBulkCopyColumnMapping> GetColumnMappings()
        {
            DataTableFieldsMapper mapper = new DataTableFieldsMapper(this.instanceDataDataTable);
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
            return this.ColumnMappings;
        }
        public  DataTable createDataTable(List<InstanceDataDTO> instanceDataDTOs)
        {
            Loggers.SVP.Info("Create instancedata data table");
            DataTable dataTable = new ListToDataTableConverter().ToDataTable(instanceDataDTOs, "`instancedata`");
            this.instanceDataDataTable = dataTable;
            return dataTable;
        }
    }
}
