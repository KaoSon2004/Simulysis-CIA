using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class SystemDTO : AbstractDTO
    {
        public long Id { get; set; }
        public string BlockType { get; set; }
        public string Name { get; set; }
        public string SID { get; set; }
        public long FK_ParentSystemId { get; set; }
        public long FK_ProjectFileId { get; set; }
        public string Properties { get; set; }
        public string SourceBlock { get; set; }
        public string SourceFile { get; set; }
        public string GotoTag { get; set; }
        public string ConnectedRefSrcFile { get; set; }
        public long FK_FakeProjectFileId { get; set; }
        public long FK_NewVersionProjectFileID { get; set; }
        /**
         * End of entity
         */
        public string ContainingFile { get; set; }

        public SystemDTO ConnectedRefSys { get; set; }

        public string ParentSystemName { get; set; }

        public List<SystemDTO> ConnectedSystemFrom { get; set; }

        public List<SystemDTO> ConnectedSystemTo { get; set; }

        /**
         * The default FK_ParentSystemId of a system is 0 otherwise its parent's Id
         * The FK_NewVersionProjectFileID is used when compared.
         */
        public SystemDTO()
        {
            FK_FakeProjectFileId = -1;
            FK_ParentSystemId = 0;
            FK_NewVersionProjectFileID = -1;
        }

    }
}