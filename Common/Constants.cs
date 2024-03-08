using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class Constants
    {
        #region Encryption constants

        public const string ENCRYPT_PASS_PHRASE = "8j&e$M3d5AfK12"; // can be any string
        public const string ENCRYPT_SALT_VALUE = "3$khk&*()ui@w"; // can be any string
        public const string ENCRYPT_HASH_ALGORITHM = "SHA1"; // can be "MD5"
        public const int ENCRYPT_PASSWORD_ITERATIONS = 4; // can be any number
        public const string ENCRYPT_INIT_VECTOR = "@1B2KSIEODfge#$%"; // must be 16 bytes
        public const int ENCRYPT_KEY_SIZE = 256; // can be 192 or 128
        public const int ENCRYPT_KEY_SIZE_SMALL = 128; // can be 192 or 128

        #endregion Encryption constants

        public const string AUTHENTICATION_COOKIE_NAME = "SVPAuthCookie";

        public const int EVENTLOG_EVENTID_SVP = 10000;

        #region Project constants

        public const string UPLOADED_PROJ_ROOT_PATH = "UploadedProjects";
        public const string GIT_REMOTE_CLONE = "RemoteGitClone";

        public const string LOG_PATH = "~/Logs";

        public const string SLX_EXTRACT_FOLDER = "temp";

        public const string PROJ_UP_SUCCESS = "Project uploaded successfully!";

        public const string PROJ_UP_FAIL = "Project upload failed!";

        public const string PROJ_UP_FAIL_EXT = "Project upload failed! Please check the file extension!";

        public const string PROJ_ALREADY_EXISTS = "Project name already exists. Please choose another name!";

        public const string PROJ_DEL_NONE = "Please select projects to delete!";

        public const string PROJ_DEL_SUCCESS = "Projects deleted successfully!";

        #endregion
        public const string PROJECT_VERSION = "1.0.24.0";

        #region Project file constants

        public const string FILE_DEL_NONE = "Please select files to delete!";

        public const string FILE_DEL_SUCCESS = "Files deleted successfully!";

        public const string FILE_UP_SUCCESS = "File uploaded successfully!";

        public const string FILE_EDIT_SUCCESS = "File edited successfully!";

        public const string FILE_UP_FAIL = "File upload failed!";

        public const string FILE_EDIT_FAIL = "File edit failed!";

        public const string FILE_UP_FAIL_EXT = "File upload failed! Please check the file extension!";

        public const string FILE_UP_EXIST = "Please Try again. File already exists!";

        #endregion

        public const string TEMPDATA_DEL_STATUS_PROP = "DeleteStatus";

        public const string TEMPDATA_DEL_MSG_PROP = "DeletedMessage";

        #region Block type constants
        
        public const string INPORT = "Inport";
        public const string OUTPORT = "Outport";
        public const string FROM = "From";
        public const string GOTO = "Goto";
        public const string REF = "Reference";
        public const string MATLAB_LIB_REF = "simulink";
        public const string MODEL_REF = "ModelReference";
        public const string SUBSYSTEM = "SubSystem";
        public const string MC_BACKUPRAM = "MC_BackUpRAM";
        public const string MC_EEPROM = "MC_EEPROM";
        public const string MSK_PREPROCESSORIF = "MSK_PreProcessorIf";
        public const string MSK_SATURATION = "MSK_Saturation";
        public const string MSK_TABLE = "MSK_Table";
        public const string MSK_MAP = "MSK_Map";
        public const string MSK_TABLE_I = "MSK_Table_i";
        public const string MSK_MAP_I = "MSK_Map_i";
        public const string MSK_INTERPOLATE1D = "MSK_Interpolate1D";
        public const string MSK_INTERPOLATE2D = "MSK_Interpolate2D";
        public const string MSK_INTERPOLATE1D_I = "MSK_Interpolate1D_i";
        public const string MSK_INTERPOLATE2D_I = "MSK_Interpolate2D_i";
        public const string MSK_GAIN = "MSK_Gain";
        public const string MSK_CONSTANT = "MSK_Constant";
        public const string MSK_INDEX = "MSK_Index";
        #endregion

        public const string SIMULINK_PARAM = "Simulink.Parameter";

        public const string INOUT_VIEW_TYPE = "inOut";
        public const string CALI_VIEW_TYPE = "calibration";

        public const string IN_VIEW_SCOPE = "inView";
        public const string IN_PROJECT_SCOPE = "inProject";
    }
}