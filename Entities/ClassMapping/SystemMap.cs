using CsvHelper.Configuration;
using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.ClassMapping
{
    public sealed class SystemMap : ClassMap<SystemDTO>
    {
        public SystemMap()
        {
            Map(m => m.Id).Name("Id");
            Map(m => m.BlockType).Name("BlockType");
            Map(m => m.Name).Name("Name");
            Map(m => m.SID).Name("sid");
            Map(m => m.FK_ParentSystemId).Name("FK_ParentSystemId");
            Map(m => m.FK_ProjectFileId).Name("FK_ProjectFileId");
            Map(m => m.Properties).Name("Properties");
            Map(m => m.SourceBlock).Name("SourceBlock");
            Map(m => m.SourceFile).Name("SourceFile");
            Map(m => m.GotoTag).Name("GotoTag");
            Map(m => m.ConnectedRefSrcFile).Name("ConnectedRefSrcFile");
            Map(m => m.FK_FakeProjectFileId).Name("FK_FakeProjectFileId");
        }
    }
}
