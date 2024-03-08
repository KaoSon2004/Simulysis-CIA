using DAO.DAO.SqlServerDAO.FileContent;
using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulysis.Helpers.SignalSearch
{
    public abstract class SignalSearchStrategy
    {
        public abstract List<Signal> Search(SearchInput input);
        
    }
}
