INSERT INTO config ("Name","Key","Value","ParentId") SELECT '�������֪ͨ','Debug','false',14 WHERE NOT EXISTS (SELECT 1 FROM config WHERE ParentId=14 AND "Key"='Notice')