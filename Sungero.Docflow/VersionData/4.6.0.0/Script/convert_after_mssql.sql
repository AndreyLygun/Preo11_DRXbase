if exists(select 1 from information_schema.tables where table_schema = 'dbo' and table_name = 'Sungero_Docflow_Params') 
  if exists(select 1 from dbo.Sungero_Docflow_Params where [Key] = 'GrantRightsMode')
    delete from dbo.Sungero_Docflow_Params where [Key] = 'GrantRightsMode'
