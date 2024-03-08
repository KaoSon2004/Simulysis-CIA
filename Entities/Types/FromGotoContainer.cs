using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace Entities.Types
{
    public class FromGotoContainer
    {
        public ConcurrentDictionary<string, ConcurrentBag<SystemDTO>> FromDict { get; set; }
        public ConcurrentDictionary<string, ConcurrentBag<SystemDTO>> GotoDict { get; set; }

        public List<SystemDTO> Systems { get; set; }
        public List<LineDTO> Lines { get; set; }

        public FromGotoContainer(List<SystemDTO> systems, List<LineDTO> lines)
        {
            FromDict = new ConcurrentDictionary<string, ConcurrentBag<SystemDTO>>();
            GotoDict = new ConcurrentDictionary<string, ConcurrentBag<SystemDTO>>();
            Systems = systems;
            Lines = lines;
        }
    }
}