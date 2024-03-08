using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class CalibrationDTO : AbstractDTO
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public decimal Value { get; set; }

        public string Description { get; set; }

        public string DataType { get; set; }

        public long FK_ProjectId { get; set; }
    }
}