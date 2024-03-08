using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class LineDTO :AbstractDTO
    {
        public long Id { get; set; }
        public long FK_SystemId { get; set; }

        public long FK_ProjectFileId { get; set; }

        public String Properties { get; set; }


    }
}
