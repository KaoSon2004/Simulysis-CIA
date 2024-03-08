using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simulysis.Helpers.SignalSearch
{
    /*
     * -To find with from-goto type, input must contain : name,projectfileId
     * -To find with inport-outport type,input must contain
     * +,With full project : name,projectId
     * +,With a set of projectfile : name, list<long>projectfileId
     * 
     * 
     */
    public class SearchInput
    {
        public String Name { get; set; }
        public long ProjectFileId { get; set; }
        public long ProjectId { get; set; }

        //this is for search in a range
        public List<long> ProjectFileIdsSet;

       
        public SearchInput()
        {

        }

        SearchInput(String name,List<long>ProjectFileIdsSet)
        {
            this.Name = Name;
            this.ProjectFileIdsSet = ProjectFileIdsSet;
        }

        public SearchInput(String Name, long ProjectFileId, long ProjectId)
        {
            this.Name = Name;
            this.ProjectFileId = ProjectFileId;
            this.ProjectId = ProjectId;
        }

        public SearchInput(String Name, long ProjectFileId)
        {
            this.Name = Name;
            this.ProjectFileId = ProjectFileId;
        }
      
    }
}