using Entities.DTO;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulysis.Helpers.DataSaver.EntityDataTable
{
    public class FileRelationshipDataTable
    {
        public List<MySqlBulkCopyColumnMapping> ColumnMappings { get; }

        public FileRelationshipDataTable()
        {
            ColumnMappings = new List<MySqlBulkCopyColumnMapping>();

            ColumnMappings = new List<MySqlBulkCopyColumnMapping>();
            ColumnMappings.Add(
            new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 0,
                DestinationColumn = "Id",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 1,
                DestinationColumn = "FK_ProjectFileId1",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 2,
                DestinationColumn = "FK_ProjectFileId2",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 3,
                DestinationColumn = "System1",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 4,
                DestinationColumn = "System2",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 5,
                DestinationColumn = "Count",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 6,
                DestinationColumn = "UniCount",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 7,
                DestinationColumn = "Type",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 8,
                DestinationColumn = "RelationshipType",
            });
            ColumnMappings.Add(new MySqlBulkCopyColumnMapping
            {
                SourceOrdinal = 9,
                DestinationColumn = "Name",
            });

        }

        public DataTable createDataTable(List<FilesRelationshipDTO> filesRelationshipDTOs)
        {
            DataTable dataTable = new ListToDataTableConverter().ToDataTable(filesRelationshipDTOs, "`filesrelationship`");
            return dataTable;
        }
    }
}
