-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------
-- -----------------------------------------------------
-- Schema svp
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema svp
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `svp` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci ;
USE `svp` ;

-- -----------------------------------------------------
-- Table `svp`.`project`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`project` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255) NULL DEFAULT NULL,
  `Path` VARCHAR(4000) NULL DEFAULT NULL,
  `Description` MEDIUMTEXT NULL DEFAULT NULL,
  `BaseProjectId` BIGINT NULL DEFAULT NULL,
  `Version` VARCHAR(255) NULL DEFAULT NULL,
  `SourceLink` VARCHAR(512) NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `Name` (`Name` ASC) VISIBLE,
  INDEX `FK_BaseProjectId_Id` (`BaseProjectId` ASC) VISIBLE,
  CONSTRAINT `FK_BaseProjectId_Id`
    FOREIGN KEY (`BaseProjectId`)
    REFERENCES `svp`.`project` (`Id`))
ENGINE = InnoDB
AUTO_INCREMENT = 796
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `svp`.`projectfile`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`projectfile` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255) NULL DEFAULT NULL,
  `Path` VARCHAR(4000) NULL DEFAULT NULL,
  `FK_ProjectId` BIGINT NOT NULL,
  `Description` LONGTEXT NULL DEFAULT NULL,
  `MathlabVersion` VARCHAR(255) NULL DEFAULT NULL,
  `SystemLevel` VARCHAR(255) NOT NULL,
  `Status` TINYINT(1) NOT NULL DEFAULT '1',
  `ErrorMsg` MEDIUMTEXT NULL DEFAULT NULL,
  `LevelVariant` VARCHAR(255) NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_ProjectId` (`FK_ProjectId` ASC) VISIBLE,
  CONSTRAINT `FK_ProjectId`
    FOREIGN KEY (`FK_ProjectId`)
    REFERENCES `svp`.`project` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 18531
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `svp`.`branch`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`branch` (
  `Id` BIGINT NOT NULL,
  `FK_LineId` BIGINT NULL DEFAULT NULL,
  `FK_ProjectFileId` BIGINT NOT NULL,
  `Properties` LONGTEXT NULL DEFAULT NULL,
  `FK_BranchId` BIGINT NULL DEFAULT NULL,
  PRIMARY KEY USING BTREE (`Id`, `FK_ProjectFileId`),
  INDEX `FK_ProjectFileId_Branch` (`FK_ProjectFileId` ASC) VISIBLE,
  CONSTRAINT `FK_ProjectFileId_Branch`
    FOREIGN KEY (`FK_ProjectFileId`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `svp`.`calibration`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`calibration` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255) NOT NULL,
  `Value` DECIMAL(20,10) NOT NULL,
  `Description` MEDIUMTEXT NULL DEFAULT NULL,
  `DataType` VARCHAR(255) NOT NULL,
  `FK_ProjectId` BIGINT NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_Project_Calibration` (`FK_ProjectId` ASC) VISIBLE,
  CONSTRAINT `FK_Project_Calibration`
    FOREIGN KEY (`FK_ProjectId`)
    REFERENCES `svp`.`project` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 4389
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `svp`.`configuration`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`configuration` (
  `Key` VARCHAR(255) CHARACTER SET 'utf8' NULL DEFAULT NULL,
  `Value` MEDIUMTEXT CHARACTER SET 'utf8' NULL DEFAULT NULL)
ENGINE = MyISAM
DEFAULT CHARACTER SET = latin1;


-- -----------------------------------------------------
-- Table `svp`.`filesrelationship`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`filesrelationship` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `FK_ProjectFileId1` BIGINT NOT NULL,
  `FK_ProjectFileId2` BIGINT NOT NULL,
  `System1` VARCHAR(255) CHARACTER SET 'utf8' NULL DEFAULT NULL,
  `System2` VARCHAR(255) CHARACTER SET 'utf8' NULL DEFAULT NULL,
  `Count` INT NOT NULL,
  `UniCount` INT NOT NULL DEFAULT '1',
  `Type` SMALLINT NULL DEFAULT NULL,
  `RelationshipType` SMALLINT NULL DEFAULT NULL,
  `Name` VARCHAR(255) CHARACTER SET 'utf8' NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_ProjectFileId1` (`FK_ProjectFileId1` ASC) VISIBLE,
  INDEX `FK_ProjectFileId2` (`FK_ProjectFileId2` ASC) VISIBLE,
  CONSTRAINT `FK_ProjectFileId1`
    FOREIGN KEY (`FK_ProjectFileId1`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_ProjectFileId2`
    FOREIGN KEY (`FK_ProjectFileId2`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 718979
DEFAULT CHARACTER SET = latin1;


-- -----------------------------------------------------
-- Table `svp`.`instancedata`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`instancedata` (
  `Id` BIGINT NOT NULL,
  `FK_SystemId` BIGINT NOT NULL,
  `FK_ProjectFileId` BIGINT NOT NULL,
  `Properties` LONGTEXT NULL DEFAULT NULL,
  PRIMARY KEY USING BTREE (`Id`, `FK_ProjectFileId`),
  INDEX `FK_ProjectFileId_InstanceData` (`FK_ProjectFileId` ASC) VISIBLE,
  CONSTRAINT `FK_ProjectFileId_InstanceData`
    FOREIGN KEY (`FK_ProjectFileId`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `svp`.`itemdetail`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`itemdetail` (
  `ID` INT NOT NULL,
  `value` INT NULL DEFAULT NULL,
  `length` INT NULL DEFAULT NULL,
  `breadth` INT NULL DEFAULT NULL,
  PRIMARY KEY (`ID`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `svp`.`line`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`line` (
  `Id` BIGINT NOT NULL,
  `FK_SystemId` BIGINT NOT NULL,
  `FK_ProjectFileId` BIGINT NOT NULL,
  `Properties` LONGTEXT NULL DEFAULT NULL,
  PRIMARY KEY USING BTREE (`Id`, `FK_ProjectFileId`),
  INDEX `FK_ProjectFileId_Line` (`FK_ProjectFileId` ASC) VISIBLE,
  CONSTRAINT `FK_ProjectFileId_Line`
    FOREIGN KEY (`FK_ProjectFileId`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `svp`.`list`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`list` (
  `Id` BIGINT NOT NULL,
  `FK_SystemId` BIGINT NOT NULL,
  `FK_ProjectFileId` BIGINT NOT NULL,
  `Properties` LONGTEXT NULL DEFAULT NULL,
  PRIMARY KEY USING BTREE (`Id`, `FK_ProjectFileId`),
  INDEX `FK_ProjectFileId_List` (`FK_ProjectFileId` ASC) VISIBLE,
  CONSTRAINT `FK_ProjectFileId_List`
    FOREIGN KEY (`FK_ProjectFileId`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `svp`.`port`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`port` (
  `Id` BIGINT NOT NULL,
  `FK_SystemId` BIGINT NOT NULL,
  `FK_ProjectFileId` BIGINT NOT NULL,
  `Properties` LONGTEXT NULL DEFAULT NULL,
  PRIMARY KEY USING BTREE (`Id`, `FK_ProjectFileId`),
  INDEX `FK_ProjectFileId_Port` (`FK_ProjectFileId` ASC) VISIBLE,
  CONSTRAINT `FK_ProjectFileId_Port`
    FOREIGN KEY (`FK_ProjectFileId`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `svp`.`system`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `svp`.`system` (
  `Id` BIGINT NOT NULL,
  `BlockType` VARCHAR(255) NULL DEFAULT NULL,
  `Name` VARCHAR(255) NULL DEFAULT NULL,
  `sid` VARCHAR(255) NULL DEFAULT NULL,
  `FK_ParentSystemId` BIGINT NOT NULL,
  `FK_ProjectFileId` BIGINT NOT NULL,
  `Properties` LONGTEXT NULL DEFAULT NULL,
  `SourceBlock` MEDIUMTEXT NULL DEFAULT NULL,
  `SourceFile` MEDIUMTEXT NULL DEFAULT NULL,
  `GotoTag` VARCHAR(255) NULL DEFAULT NULL,
  `ConnectedRefSrcFile` VARCHAR(255) CHARACTER SET 'utf8' NULL DEFAULT NULL,
  `FK_FakeProjectFileId` BIGINT NULL DEFAULT NULL,
  PRIMARY KEY (`Id`, `FK_ProjectFileId`),
  INDEX `FK_ProjectFileId_System` (`FK_ProjectFileId` ASC) VISIBLE,
  INDEX `FK_FakeProjectFileId_System_idx` (`FK_FakeProjectFileId` ASC) VISIBLE,
  CONSTRAINT `FK_FakeProjectFileId_System`
    FOREIGN KEY (`FK_FakeProjectFileId`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_ProjectFileId_System`
    FOREIGN KEY (`FK_ProjectFileId`)
    REFERENCES `svp`.`projectfile` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;

USE `svp` ;

-- -----------------------------------------------------
-- procedure create_files_relationship
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_files_relationship`(i_FK_ProjectFileId1 bigint,i_FK_ProjectFileId2 bigint,i_Count bigint,i_Type smallint,i_RelationshipType smallint)
BEGIN
	INSERT INTO FilesRelationship(FK_ProjectFileId1,FK_ProjectFileId2,Count,Type,RelationshipType) VALUES (i_FK_ProjectFileId1,i_FK_ProjectFileId2,i_Count,i_Type,i_RelationshipType);
    SELECT LAST_INSERT_ID();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure create_new_branch
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_new_branch`(i_Fk_LineId bigint,i_Properties mediumtext,i_FK_BranchId bigint)
BEGIN
	INSERT INTO tranh414_SVP.Branch(Fk_LineId,Properties,FK_BranchId) VALUES(i_Fk_LineId,i_Properties,i_FK_BranchId);
    SELECT LAST_INSERT_ID();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure create_new_instancedata
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_new_instancedata`(i_FK_SystemId bigint,i_Properties mediumtext)
BEGIN
	INSERT INTO tranh414_SVP.InstanceData(FK_SystemId,Properties) VALUES(i_FK_SystemId,i_Properties);
    SELECT LAST_INSERT_ID();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure create_new_line
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_new_line`(i_FK_SystemId bigint,i_Properties mediumtext)
BEGIN
	INSERT INTO tranh414_SVP.Line(FK_SystemId,Properties) VALUES(i_FK_SystemId,i_Properties);
    SELECT LAST_INSERT_ID();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure create_new_list
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_new_list`(i_FK_SystemId bigint,i_Properties mediumtext)
BEGIN
	INSERT INTO tranh414_SVP.List(FK_SystemId,Properties) VALUES(i_FK_SystemId,i_Properties);
    SELECT LAST_INSERT_ID();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure create_new_port
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_new_port`(i_FK_SystemId bigint,i_Properties mediumtext)
BEGIN
	INSERT INTO tranh414_SVP.Port(FK_SystemId,Properties) VALUES(i_FK_SystemId,i_Properties);
    SELECT LAST_INSERT_ID();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure create_new_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_new_project`(i_name VARCHAR(255),i_description mediumtext,i_path VARCHAR(4000), i_baseProjectId BIGINT, i_version VARCHAR(255))
BEGIN
	INSERT INTO Project(name,description,path,BaseProjectId,Version) VALUES (i_name,i_description,i_path,i_baseProjectId,i_version);
    SELECT last_insert_id();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure create_new_system
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_new_system`(i_FK_ParentSystemId bigint,i_BlockType varchar(255),i_name varchar(255),i_SID varchar(255),i_FK_ProjectFileId bigint,i_Properties mediumtext )
BEGIN
	INSERT INTO tranh414_SVP.`System`(FK_ParentSystemId,BlockType,Name,SID,FK_ProjectFileId,Properties) VALUES (i_FK_ParentSystemId,i_BlockType,i_name,i_SID,i_FK_ProjectFileId,i_Properties);
    SELECT LAST_INSERT_ID();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure create_project_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `create_project_file`(IN i_Name VARCHAR(255) CHARSET utf8, IN i_Path VARCHAR(4000), IN i_FK_ProjectId BIGINT(20), IN i_Description LONGTEXT, IN i_MathlabVersion VARCHAR(255), IN i_SystemLevel VARCHAR(255), IN i_Status BOOLEAN, IN i_ErrorMsg MEDIUMTEXT)
BEGIN
	INSERT INTO ProjectFile (Name, Path, FK_ProjectId, Description, MathlabVersion, SystemLevel, Status, ErrorMsg)
	VALUES (i_Name, i_Path, i_FK_ProjectId, i_Description, i_MathlabVersion, i_SystemLevel, i_Status, i_ErrorMsg);
    SELECT last_insert_id();
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure delete_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `delete_file`(i_Id bigint)
BEGIN
	DELETE FROM ProjectFile
    WHERE ProjectFile.Id = i_Id;
    
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure delete_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `delete_project`(i_Id bigint)
BEGIN
	DELETE FROM Project 
    WHERE Project.Id = i_Id;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure edit_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `edit_file`(i_Id bigint,i_Description mediumtext,i_SystemLevel varchar(255))
BEGIN
	UPDATE ProjectFile
SET Description = i_Description, SystemLevel = i_SystemLevel
WHERE Id=i_Id;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure find_parent_system
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `find_parent_system`(IN `i_FK_ParentSystemId` BIGINT, IN `i_FK_ProjectFileId` BIGINT)
SELECT * FROM `System`
WHERE FK_ProjectFileId = i_FK_ProjectFileId
AND Id = i_FK_ParentSystemId$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure find_projectfiles_by_file_level
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `find_projectfiles_by_file_level`(i_FileLevel varchar(255), i_ProjectId bigint)
BEGIN
	SELECT * 
    FROM ProjectFile 
    WHERE ProjectFile.SystemLevel = i_FileLevel
    AND ProjectFile.FK_ProjectId = i_ProjectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_branches_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_branches_in_project`(i_projectId BIGINT)
BEGIN
	SELECT * 
    FROM branch
    JOIN ProjectFile
    ON (branch.FK_ProjectFileId = ProjectFile.Id)
    WHERE ProjectFile.FK_ProjectId = i_projectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_file_of_level_in_projects
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_file_of_level_in_projects`(
    IN i_projectId BIGINT,
    IN s_level MEDIUMTEXT
)
BEGIN
    SELECT Id
    FROM ProjectFile
    WHERE (ProjectFile.FK_ProjectId = i_projectId)
    AND (ProjectFile.SystemLevel = s_level);
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_from_goto_systems_in_a_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_from_goto_systems_in_a_file`(IN `i_FK_ProjectFileId` BIGINT)
SELECT `System`.*,ProjectFile.Name as ContainingFile
FROM
`System`
JOIN
ProjectFile
ON `System`.FK_ProjectFileId = i_FK_ProjectFileId
AND BlockType IN ('From','Goto')$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_inport_ouport_system_in_a_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_inport_ouport_system_in_a_file`(IN `i_FK_ProjectFileId` BIGINT)
SELECT 
`System`.*,ProjectFile.Name as ContainingFile
FROM
`System`
JOIN
ProjectFile
ON 
`System`.FK_ProjectFileId = ProjectFile.Id
WHERE 
`System`.BlockType In ('Inport','Outport')
AND `System`.FK_ProjectFileId = i_FK_ProjectFileId$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_inport_ouport_system_in_a_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_inport_ouport_system_in_a_project`(IN `i_FK_ProjectId` BIGINT)
SELECT `System`.* , ProjectFile.Name As ContainingFile
FROM
	`System` 
JOIN
ProjectFile
ON `System`.FK_ProjectFileId = ProjectFile.Id
WHERE 
`System`.BlockType in ('Inport','Outport')
AND ProjectFile.FK_ProjectId = i_FK_ProjectId$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_instancedatas_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_instancedatas_in_project`(i_projectId BIGINT)
BEGIN
	SELECT * 
    FROM instancedata
    JOIN  projectfile
    ON(instancedata.FK_ProjectFileId = projectfile.id)
    WHERE projectfile.FK_ProjectId = i_projectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_lines_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_lines_in_project`(i_projectId BIGINT)
BEGIN
	SELECT * 
    FROM line
    JOIN ProjectFile
    ON (line.FK_ProjectFileId = ProjectFile.Id)
    WHERE ProjectFile.FK_ProjectId = i_projectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_lists_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_lists_in_project`(i_projectId BIGINT)
BEGIN
	SELECT * 
    FROM list
    JOIN ProjectFile
    ON (list.FK_ProjectFileId = ProjectFile.Id)
    WHERE ProjectFile.FK_ProjectId = i_projectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_ports_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_ports_in_project`(i_projectId BIGINT)
BEGIN
	SELECT * 
    FROM port
    JOIN ProjectFile
    ON (port.FK_ProjectFileId = ProjectFile.Id)
    WHERE ProjectFile.FK_ProjectId = i_projectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_all_systems_in_a_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_all_systems_in_a_project`(i_projectId BIGINT)
BEGIN
	SELECT * 
    FROM `system`
    JOIN ProjectFile
    ON (`system`.FK_ProjectFileId = ProjectFile.Id)
    WHERE ProjectFile.FK_ProjectId = i_projectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_empty_blocktype_systems_in_a_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_empty_blocktype_systems_in_a_project`(i_FK_ProjectId bigint)
BEGIN
	SELECT `System`.*,ProjectFile.Name as ContainingFile
    FROM
    `System`
    JOIN
    ProjectFile
    ON( `System`.FK_ProjectFileId = ProjectFile.Id)
    WHERE ProjectFile.FK_ProjectId = i_FK_ProjectId
    AND `System`.BlockType = "";
    
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_list_of_fileIds_in_dependency_view
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_list_of_fileIds_in_dependency_view`(IN `i_ProjectFileId` BIGINT)
SELECT * FROM FilesRelationship WHERE FilesRelationship.FK_ProjectFileId1 = i_ProjectFileId OR FilesRelationship.FK_ProjectFileId2 = i_ProjectFileId$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_project_versions
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_project_versions`(i_projectId BIGINT)
BEGIN
	DECLARE parentProjectId BIGINT;
    SET @parentProjectId = (SELECT BaseProjectId from Project WHERE (Id = i_projectId));
    
    IF @parentProjectId is NULL THEN
		SELECT * from Project WHERE (Id = i_projectId) OR (BaseProjectId = i_projectId);
    ELSE
		SELECT * from Project WHERE (Id = @parentProjectId) OR (BaseProjectId = @parentProjectId);
    END IF;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_source_block
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_source_block`()
BEGIN
	SET SQL_SAFE_UPDATES = 0;
	UPDATE tranh414_SVP.`System`
	SET tranh414_SVP.`System`.SourceBlock =   Substring(REGEXP_SUBSTR(tranh414_SVP.`System`.Properties, 'SourceBlock\\":\\"(.*?)\\"'),14)
	WHERE tranh414_SVP.`System`.BlockType =  "reference";
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_sub_file_list
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_sub_file_list`(IN `i_ProjectFileId` BIGINT, IN `i_ProjectId` INT)
SELECT DISTINCT ProjectFile.Id
FROM 
`System`
JOIN
ProjectFile
ON
`System`.SourceFile = ProjectFile.Name
AND (BlockType = 'ModelReference'OR (BlockType = 'Reference'  AND `System`.SourceFile!= 'simulink'))
AND `System`.FK_ProjectFileId = i_ProjectFileId
AND ProjectFile.FK_ProjectId = i_ProjectId$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure get_sys_and_parent_subsys_child
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_sys_and_parent_subsys_child`(IN `i_SystemId` BIGINT, IN `i_FK_ProjectFileId` BIGINT, IN `i_FK_ParentSystemId` BIGINT)
BEGIN
	SELECT * FROM `System`
	WHERE `System`.`FK_ProjectFileId` = i_FK_ProjectFileId AND (`System`.`Id` = i_SystemId OR `System`.`Id` = i_FK_ParentSystemId OR (`System`.`FK_ParentSystemId` = i_SystemId + 1) AND `System`.`BlockType` = 'SubSystem');
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_all_file_id
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_all_file_id`(i_ProjectId bigint)
BEGIN
	SELECT Id FROM ProjectFile
    WHERE ProjectFile.FK_ProjectId = i_ProjectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_all_files
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_all_files`(i_projectId bigint)
BEGIN
	SELECT * FROM ProjectFile WHERE ProjectFile.FK_ProjectId = i_projectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_all_projects
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_all_projects`()
BEGIN
	SELECT  * FROM Project
    ORDER BY Id DESC;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_branches
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_branches`(i_FK_ProjectFileId bigint)
BEGIN
	SELECT * FROM Branch
    WHERE Branch.FK_ProjectFileId=i_FK_ProjectFileId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_file_by_id
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_file_by_id`(i_Id bigint)
BEGIN
	SELECT * FROM ProjectFile
    Where ProjectFile.Id = i_Id;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_file_by_name
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_file_by_name`(i_Name varchar(255),i_Id bigint)
BEGIN
   SELECT * FROM ProjectFile WHERE Name = i_Name AND FK_ProjectId = i_Id;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_files_relationship
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_files_relationship`(IN `i_FK_ProjectFileId1` BIGINT, IN `i_FK_ProjectFileId2` BIGINT, IN `i_RelationshipType` SMALLINT)
BEGIN
	SELECT * FROM `FilesRelationship` WHERE ((`FilesRelationship`.`FK_ProjectFileId1` = i_FK_ProjectFileId1 AND `FilesRelationship`.`FK_ProjectFileId2` = i_FK_ProjectFileId2) OR (`FilesRelationship`.`FK_ProjectFileId1` = i_FK_ProjectFileId2 AND `FilesRelationship`.`FK_ProjectFileId2` = i_FK_ProjectFileId1)) AND `FilesRelationship`.`RelationshipType` = i_RelationshipType;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_instancedatas
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_instancedatas`(i_FK_ProjectFileId bigint)
BEGIN
	SELECT * FROM InstanceData
    WHERE FK_ProjectFileId = i_FK_ProjectFileId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_lines
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_lines`(i_FK_ProjectFileId bigint)
BEGIN
	SELECT * FROM Line 
    WHERE Line.FK_ProjectFileId = i_FK_ProjectFileId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_lists
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_lists`(i_FK_ProjectFileId bigint)
BEGIN
	SELECT * FROM List 
    WHERE List.FK_ProjectFileId = i_FK_ProjectFileId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_log_level
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_log_level`(i_key varchar(255))
BEGIN
	SELECT * FROM Configuration
    WHERE Configuration.Key = i_key;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_parent_equal_file_relationships
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_parent_equal_file_relationships`(IN `fileId` BIGINT(20))
SELECT * FROM FilesRelationship WHERE FK_ProjectFileId2 = fileId OR FK_ProjectFileId1 = fileId ORDER BY Type DESC$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_ports
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_ports`(i_FK_ProjectFileId bigint)
BEGIN
	SELECT * FROM Port
    WHERE Port.FK_ProjectFileId = i_FK_ProjectFileId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_project_by_id
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_project_by_id`(i_Id bigint)
BEGIN
	SELECT * FROM Project
    WHERE Project.Id = i_Id;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_project_by_name
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_project_by_name`(i_name VARCHAR(255))
BEGIN
	SELECT * 
    FROM Project
    WHERE Project.name=i_name;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure read_systems
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `read_systems`(i_FK_ProjectFileId bigint)
BEGIN
	SELECT *
    FROM `System`
    WHERE `System`.FK_ProjectFileId= i_FK_ProjectFileId;
    
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure save_log_level
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `save_log_level`(i_key varchar(255),i_value mediumtext)
BEGIN
	UPDATE Configuration
    SET Value = i_value
    WHERE i_Key = Configuration.Key;
    
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_calibration_by_name
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_calibration_by_name`(IN `Name` VARCHAR(255), IN `FK_ProjectId` BIGINT(20))
SELECT * FROM Calibration
	WHERE Calibration.FK_ProjectId = FK_ProjectId
	AND Calibration.Name LIKE concat("%", Name, "%")$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_inport_outport_system_by_name_in_a_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_inport_outport_system_by_name_in_a_file`(IN `i_Name` VARCHAR(255), IN `i_FK_ProjectFileId` BIGINT)
SELECT `System`.*,ProjectFile.Name as ContainingFile
FROM `System` JOIN ProjectFile
    ON(`System`.FK_ProjectFileId = ProjectFile.Id)
	WHERE `System`.FK_ProjectFileId = i_FK_ProjectFileId
	AND `System`.Name Like concat("%", i_Name, "%")
    AND `System`.BlockType IN ("Outport", "Inport")$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_inport_outport_system_by_name_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_inport_outport_system_by_name_in_project`(IN `i_Name` VARCHAR(255), IN `i_ProjectId` BIGINT)
SELECT `System`.*,ProjectFile.Name as ContainingFile  FROM 
`System`
JOIN ProjectFile
ON(`System`.FK_ProjectFileId = ProjectFile.Id)
WHERE ProjectFile.FK_ProjectId = i_ProjectId
AND `System`.BlockType IN ('Inport','Outport')
AND `System`.Name Like Concat("%",i_Name,"%")$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_matched_inport_outport_system_in_a_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_matched_inport_outport_system_in_a_file`(IN `i_Name` VARCHAR(255), IN `i_BlockType` VARCHAR(255), IN `i_FK_ProjectFileId` BIGINT)
SELECT 
`System`.*,ProjectFile.Name as ContainingFile
FROM
`System`
JOIN
ProjectFile
ON
`System`.FK_ProjectFileId = ProjectFile.Id
WHERE `System`.FK_ProjectFileId = i_FK_ProjectFileId
AND `System`.BlockType = i_BlockType
AND `System`.Name = i_Name$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_matched_inport_outport_system_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_matched_inport_outport_system_in_project`(IN `i_FK_ProjectId` BIGINT, IN `i_BlockType` VARCHAR(255), IN `i_Name` VARCHAR(255))
SELECT `System`.*,ProjectFile.Name as ContainingFile
FROM `System`
JOIN ProjectFile
ON(`System`.FK_ProjectFileId = ProjectFile.Id)
WHERE ProjectFile.FK_ProjectId = i_FK_ProjectId
AND `System`.BlockType = i_BlockType
AND `System`.Name = i_Name$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_matched_system
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_matched_system`(IN `i_GotoTag` VARCHAR(255), IN `i_BlockType` VARCHAR(255), IN `i_FK_ProjectFileId` BIGINT)
SELECT * FROM `System`
	WHERE `System`.FK_ProjectFileId = i_FK_ProjectFileId
	AND BlockType = i_BlockType 
    AND GotoTag = i_GotoTag$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_system_by_calibration_in_a_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_system_by_calibration_in_a_file`(IN `i_Calibration` VARCHAR(255), IN `i_FK_ProjectFileId` BIGINT(20), IN `i_DataType` VARCHAR(255))
SELECT `System`.*, ProjectFile.Name as ContainingFile
	FROM `System` JOIN ProjectFile
	ON `System`.FK_ProjectFileId = ProjectFile.Id
	WHERE `System`.FK_ProjectFileId = i_FK_ProjectFileId AND `System`.Properties LIKE BINARY concat('%"',i_DataType, '":"', i_Calibration,'"%')$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_system_by_calibration_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_system_by_calibration_in_project`(IN `i_Calibration` VARCHAR(255), IN `i_FK_ProjectId` BIGINT(20), IN `i_DataType` VARCHAR(255))
SELECT `System`.*, ProjectFile.Name as ContainingFile
	FROM `System` JOIN ProjectFile
	ON `System`.FK_ProjectFileId = ProjectFile.Id
	WHERE ProjectFile.FK_ProjectId = i_FK_ProjectId AND `System`.Properties LIKE BINARY concat('%"',i_DataType, '":"', i_Calibration,'"%')$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_system_by_goto_tag_in_a_file
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_system_by_goto_tag_in_a_file`(IN `i_GotoTag` VARCHAR(255), IN `i_FK_ProjectFileId` BIGINT)
SELECT `System`.*,ProjectFile.Name as ContainingFile
FROM `System`
JOIN ProjectFile
ON (`System`.FK_ProjectFileId = ProjectFile.Id)
WHERE `System`.GotoTag Like Concat("%",i_GotoTag,"%")
AND Blocktype IN ('From','GoTo')
AND `System`.FK_ProjectFileId = i_FK_ProjectFileId$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure search_system_by_goto_tag_in_project
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `search_system_by_goto_tag_in_project`(IN `i_GotoTag` VARCHAR(255), IN `i_FK_ProjectId` BIGINT)
BEGIN
	SELECT  `System`.*,ProjectFile.Name as ContainingFile
	FROM `System`
	JOIN ProjectFile
	ON (`System`.FK_ProjectFileId = ProjectFile.Id)
    JOIN Project
    On (ProjectFile.FK_ProjectId = i_FK_ProjectId)
	WHERE `System`.GotoTag Like Concat("%",i_GotoTag,"%")
    AND Blocktype IN ('From','GoTo')
    AND ProjectFile.FK_ProjectId = i_FK_ProjectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure update_project_file_status
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `update_project_file_status`(IN `i_FileId` BIGINT, IN `i_Status` BOOLEAN, IN `i_ErrorMsg` MEDIUMTEXT)
UPDATE ProjectFile
SET ProjectFile.Status = i_Status, ProjectFile.ErrorMsg = i_ErrorMsg
WHERE ProjectFile.Id = i_FileId$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure update_project_git_link
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `update_project_git_link`(i_ProjectId BIGINT, i_Link VARCHAR(512))
BEGIN
	UPDATE Project SET SourceLink = i_Link WHERE Id = i_ProjectId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure update_systemlevel
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `update_systemlevel`(i_ProjectId bigint,i_ProjectFileId bigint,i_SystemLevel varchar(255))
BEGIN
	UPDATE ProjectFile
    SET ProjectFile.SystemLevel = i_SystemLevel
    WHERE ProjectFile.FK_ProjectId = i_ProjectId
    AND ProjectFile.Id = i_ProjectFileId;
END$$

DELIMITER ;

-- -----------------------------------------------------
-- procedure update_systemlevelvariant
-- -----------------------------------------------------

DELIMITER $$
USE `svp`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `update_systemlevelvariant`(IN `i_ProjectId` BIGINT, IN `i_ProjectFileId` BIGINT, IN `i_Variant` MEDIUMTEXT)
BEGIN
UPDATE ProjectFile
SET ProjectFile.LevelVariant = i_Variant
WHERE (ProjectFile.Id = i_ProjectFileId) AND (ProjectFile.FK_ProjectId = i_ProjectId);
END$$

DELIMITER ;

SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
