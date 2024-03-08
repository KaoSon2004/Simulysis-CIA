use svp;
ALTER TABLE project
ADD SourceLink VARCHAR(512) DEFAULT NULL;

DELIMITER $$

CREATE PROCEDURE `update_project_git_link`(i_ProjectId BIGINT, i_Link VARCHAR(512))
BEGIN
	UPDATE Project SET SourceLink = i_Link WHERE Id = i_ProjectId;
END $$

DELIMITER ;