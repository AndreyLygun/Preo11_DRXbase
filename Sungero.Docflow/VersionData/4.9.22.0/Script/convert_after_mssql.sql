IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_DocumentGroup')
BEGIN
  DROP INDEX idx_EDoc_DocumentGroup ON Sungero_Content_EDoc  
END
