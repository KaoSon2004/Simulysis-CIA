using Entities.DTO;
using Entities.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulysis.Helpers.DataSaver.EntityDataTable
{
    public class BranchDataTable : DTODataTable
    {
        public List<MySqlBulkCopyColumnMapping> ColumnMappings { get; set; }

        public DataTable branchDataTable { get; set; }

        public BranchDataTable()
        {

        }

        public override List<MySqlBulkCopyColumnMapping> GetColumnMappings()
        {

            ColumnMappings = new List<MySqlBulkCopyColumnMapping>();
            DataTableFieldsMapper mapper = new DataTableFieldsMapper(this.branchDataTable);

            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("Id"),
                DestinationColumn = "Id",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("FK_LineId"),
                DestinationColumn = "FK_LineId",
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
            }) ;
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = mapper.getOrderOf("FK_BranchId"),
                DestinationColumn = "FK_BranchId",
            });
            return this.ColumnMappings;
        }

        public  DataTable createDataTable(List<BranchDTO> branchDTOs)
        {
            Loggers.SVP.Info("Create branch data table");
            DataTable dataTable = new ListToDataTableConverter().ToDataTable(branchDTOs, "`branch`");
            this.branchDataTable = dataTable;
            return dataTable;
        }

       
    }
}
