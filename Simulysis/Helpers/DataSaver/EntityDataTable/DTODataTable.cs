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
    public abstract class DTODataTable
    {
        public abstract List<MySqlBulkCopyColumnMapping> GetColumnMappings();
    }
}
