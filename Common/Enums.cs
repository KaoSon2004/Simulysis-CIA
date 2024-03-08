using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum LoggingLevel
    {
        Error = 0,
        Verbose = 1
    }

    public enum LoggingDestination
    {
        Txt = 0,
        EventLog = 1
    }

    public enum FileRelationship
    {
        Equal = 0,
        Child_Parent = 1,
        Parent_Child = 2
    }

    public enum RelationshipType
    {
        In_Out = 0,
        From_Go_To = 1,
        Calibration = 2
    }

    public enum SearchRange
    {
        In_Project = 0,
        In_A_Set_Of_Files
    }

    public enum InitialCapacity
    {
        System = 5000,
        Line = 3000,
        List = 1500,
        FileRelationship = 1000,
        Other = 500,
    }
}