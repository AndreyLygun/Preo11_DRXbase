if not exists (select COLUMN_NAME
               from INFORMATION_SCHEMA.COLUMNS
               where table_name='Sungero_ExCore_QueueItem' 
                 and COLUMN_NAME = 'DocumentId_Docflow_Sungero'
                 and DATA_TYPE = 'bigint')
begin
  alter table dbo.Sungero_ExCore_QueueItem
  add DocumentId_Docflow_Sungero bigint
end
