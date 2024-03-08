ALTER TABLE Project
ADD BaseProjectId BIGINT;

ALTER TABLE Project
ADD CONSTRAINT FK_BaseProjectId_Id FOREIGN KEY (BaseProjectId) REFERENCES Project (Id);

DELIMITER $$

CREATE PROCEDURE `get_project_versions`(i_projectId BIGINT)
BEGIN
	DECLARE parentProjectId BIGINT;
    SET @parentProjectId = (SELECT BaseProjectId from Project WHERE (Id = i_projectId));
    
    IF @parentProjectId is NULL THEN
		SELECT * from Project WHERE (Id = i_projectId) OR (BaseProjectId = i_projectId);
    ELSE
		SELECT * from Project WHERE (Id = @parentProjectId) OR (BaseProjectId = @parentProjectId);
    END IF;
END $$

DELIMITER ;