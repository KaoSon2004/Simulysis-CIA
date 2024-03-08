using Entities.DTO;
using Entities.Logging;
using MySqlConnector;
using Simulysis.Helpers.DataSaver;
using Simulysis.Helpers.DataSaver.EntityDataTable;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.EntityDataTable
{
    public class SystemDataTable:DTODataTable
    {
        public List<MySqlBulkCopyColumnMapping> ColumnMappings { get; set; }
        public DataTable systemDataTable { get; set; }

        public SystemDataTable()
        {
        }

        public override List<MySqlBulkCopyColumnMapping> GetColumnMappings()
        {
            DataTableFieldsMapper mapper = new DataTableFieldsMapper(this.systemDataTable);

            ColumnMappings = new List<MySqlBulkCopyColumnMapping>();
            ColumnMappings.Add(
            new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("Id"),
                DestinationColumn = "Id",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("BlockType"),
                DestinationColumn = "BlockType",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("Name"),
                DestinationColumn = "Name",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("sid"),
                DestinationColumn = "sid",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("FK_ParentSystemId"),
                DestinationColumn = "FK_ParentSystemId",
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
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("SourceBlock"),
                DestinationColumn = "SourceBlock",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("SourceFile"),
                DestinationColumn = "SourceFile",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("GotoTag"),
                DestinationColumn = "GotoTag",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("ConnectedRefSrcFile"),
                DestinationColumn = "ConnectedRefSrcFile",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("FK_FakeProjectFileId"),
                DestinationColumn = "FK_FakeProjectFileId",
            });
            return this.ColumnMappings;
        }

        public  DataTable createDataTable(List<SystemDTO> systemDTOs)
        {
            Loggers.SVP.Info("Create system data table");
           
            DataTable dataTable = new ListToDataTableConverter().ToDataTable(systemDTOs, "`system`");

            Loggers.SVP.Info("After list to datatable");
            dataTable.Columns.Remove("ContainingFile");
            dataTable.Columns.Remove("ConnectedRefSys");
            dataTable.Columns.Remove("ParentSystemName");
            Loggers.SVP.Info("After remove column of system table");

          
            Loggers.SVP.Info("After filter system data table");
            this.systemDataTable = dataTable;
            return dataTable;
        }
    }
}
