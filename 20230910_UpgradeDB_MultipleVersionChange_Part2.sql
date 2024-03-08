Use svp;
ALTER TABLE Project
ADD Version VARCHAR(255) DEFAULT NULL;

DROP PROCEDURE `create_new_project`;

DELIMITER $$

CREATE PROCEDURE `create_new_project`(i_name VARCHAR(255),i_description mediumtext,i_path VARCHAR(4000), i_baseProjectId BIGINT, i_version VARCHAR(255))
BEGIN
	INSERT INTO Project(name,description,path,BaseProjectId,Version) VALUES (i_name,i_description,i_path,i_baseProjectId,i_version);
    SELECT last_insert_id();
END $$

DELIMITER ;