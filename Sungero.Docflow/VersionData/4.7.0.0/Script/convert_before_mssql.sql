-- Сконвертировать поля с типом int таблицы Sungero_DocRegister_CurrentNumbers в bigint.
if exists (select COLUMN_NAME
           from INFORMATION_SCHEMA.COLUMNS
           where table_name='Sungero_DocRegister_CurrentNumbers' 
             and COLUMN_NAME in ('DocRegisterId', 'LeadDocument', 'Department', 'BUnit') 
             and DATA_TYPE = 'int')
begin
  if exists (select * 
             from sys.indexes 
             where object_id = OBJECT_ID(N'[dbo].[Sungero_DocRegister_CurrentNumbers]') 
               and name = N'Sungero_DocRegister_CurrentCode_DocRegisterId')
    drop index Sungero_DocRegister_CurrentCode_DocRegisterId on [dbo].[Sungero_DocRegister_CurrentNumbers]
		
  if exists (select * 
             from sys.indexes 
             where object_id = OBJECT_ID(N'[dbo].[Sungero_DocRegister_CurrentNumbers]') 
               and name = N'Sungero_DocRegister_CurrentCode_DocRegisterId_Month')
    drop index Sungero_DocRegister_CurrentCode_DocRegisterId_Month on [dbo].[Sungero_DocRegister_CurrentNumbers]
		
  if exists (select * 
             from sys.indexes 
             where object_id = OBJECT_ID(N'[dbo].[Sungero_DocRegister_CurrentNumbers]') 
               and name = N'Sungero_DocRegister_CurrentCode_DocRegisterId_Month_Year')
    drop index Sungero_DocRegister_CurrentCode_DocRegisterId_Month_Year on [dbo].[Sungero_DocRegister_CurrentNumbers]
		
  if exists (select * 
             from sys.indexes 
             where object_id = OBJECT_ID(N'[dbo].[Sungero_DocRegister_CurrentNumbers]') 
               and name = N'Sungero_DocRegister_CurrentCode_DocRegisterId_Month_Year_LDoc')
    drop index Sungero_DocRegister_CurrentCode_DocRegisterId_Month_Year_LDoc on [dbo].[Sungero_DocRegister_CurrentNumbers]
  
  if exists (select * 
             from sys.indexes 
             where object_id = OBJECT_ID(N'[dbo].[Sungero_DocRegister_CurrentNumbers]') 
               and name = N'Sungero_DocRegister_CurrentNumbers_DocRegisterId')
    drop index Sungero_DocRegister_CurrentNumbers_DocRegisterId on [dbo].[Sungero_DocRegister_CurrentNumbers]
		
  if exists (select * 
             from sys.indexes 
             where object_id = OBJECT_ID(N'[dbo].[Sungero_DocRegister_CurrentNumbers]') 
               and name = N'Sungero_DocRegister_CurrentNumbers_DocRegisterId_Month')
    drop index Sungero_DocRegister_CurrentNumbers_DocRegisterId_Month on [dbo].[Sungero_DocRegister_CurrentNumbers]
		
  if exists (select * 
             from sys.indexes 
             where object_id = OBJECT_ID(N'[dbo].[Sungero_DocRegister_CurrentNumbers]') 
               and name = N'Sungero_DocRegister_CurrentNumbers_DocRegisterId_Month_Year')
    drop index Sungero_DocRegister_CurrentNumbers_DocRegisterId_Month_Year on [dbo].[Sungero_DocRegister_CurrentNumbers]
		
  if exists (select * 
             from sys.indexes 
             where object_id = OBJECT_ID(N'[dbo].[Sungero_DocRegister_CurrentNumbers]') 
               and name = N'Sungero_DocRegister_CurrentNumbers_DocRegisterId_Month_Year_LDoc')
    drop index Sungero_DocRegister_CurrentNumbers_DocRegisterId_Month_Year_LDoc on [dbo].[Sungero_DocRegister_CurrentNumbers]


  DECLARE @constraints_to_drop VARCHAR(MAX)
  SELECT @constraints_to_drop = COALESCE(@constraints_to_drop + 'alter table [dbo].[Sungero_DocRegister_CurrentNumbers] DROP CONSTRAINT [' + default_constraints.name + ']', 
                                        'alter table [dbo].[Sungero_DocRegister_CurrentNumbers] DROP CONSTRAINT [' + default_constraints.name + ']')
  FROM sys.all_columns 
  INNER JOIN sys.tables ON all_columns.object_id = tables.object_id
  INNER JOIN sys.schemas ON tables.schema_id = schemas.schema_id
  INNER JOIN sys.default_constraints ON all_columns.default_object_id = default_constraints.object_id
  WHERE schemas.name = 'dbo' 
    AND tables.name = 'Sungero_DocRegister_CurrentNumbers'

if @constraints_to_drop is not null
	exec (@constraints_to_drop)

  alter table dbo.Sungero_DocRegister_CurrentNumbers
  alter column DocRegisterId bigint
  alter table dbo.Sungero_DocRegister_CurrentNumbers
  alter column LeadDocument bigint
  alter table dbo.Sungero_DocRegister_CurrentNumbers
  alter column Department bigint
  alter table dbo.Sungero_DocRegister_CurrentNumbers
  alter column BUnit bigint
end

-- Сконвертировать поля с типом int таблицы Sungero_Contrac_ExpiringContracts в bigint.
if exists (select COLUMN_NAME
           from INFORMATION_SCHEMA.COLUMNS
           where table_name='Sungero_Contrac_ExpiringContracts' 
             and COLUMN_NAME in ('EDoc', 'Task') 
             and DATA_TYPE = 'int')
begin
  declare @expiringContracts_constraints_to_drop nvarchar(max)
  select @expiringContracts_constraints_to_drop = COALESCE(@expiringContracts_constraints_to_drop + ' alter table [dbo].[Sungero_Contrac_ExpiringContracts] DROP CONSTRAINT [' + kc.[NAME] + ']', 
                                                           'alter table [dbo].[Sungero_Contrac_ExpiringContracts] DROP CONSTRAINT [' + kc.[NAME] + ']')
  from sys.key_constraints as kc
  where parent_object_id = OBJECT_ID(N'[dbo].[Sungero_Contrac_ExpiringContracts]')

  if @expiringContracts_constraints_to_drop is not null
    exec (@expiringContracts_constraints_to_drop)

  alter table dbo.Sungero_Contrac_ExpiringContracts
  alter column EDoc bigint
  alter table dbo.Sungero_Contrac_ExpiringContracts
  alter column Task bigint
  
  alter table [dbo].[Sungero_Contrac_ExpiringContracts] add unique nonclustered ([EDoc] asc)
end

-- Сконвертировать поля с типом int таблицы Sungero_Docflow_ExpiringPoA в bigint.
if exists (select COLUMN_NAME
           from INFORMATION_SCHEMA.COLUMNS
           where table_name='Sungero_Docflow_ExpiringPoA' 
             and COLUMN_NAME in ('EDoc', 'Task') 
             and DATA_TYPE = 'int')
begin
  declare @expiringPoA_constraints_to_drop nvarchar(max)
  select @expiringPoA_constraints_to_drop = COALESCE(@expiringPoA_constraints_to_drop + ' alter table [dbo].[Sungero_Docflow_ExpiringPoA] DROP CONSTRAINT [' + kc.[NAME] + ']', 
                                                     'alter table [dbo].[Sungero_Docflow_ExpiringPoA] DROP CONSTRAINT [' + kc.[NAME] + ']')
  from sys.key_constraints as kc
  where parent_object_id = OBJECT_ID(N'[dbo].[Sungero_Docflow_ExpiringPoA]')

  if @expiringPoA_constraints_to_drop is not null
    exec (@expiringPoA_constraints_to_drop)
    
  alter table dbo.Sungero_Docflow_ExpiringPoA
  alter column EDoc bigint
  alter table dbo.Sungero_Docflow_ExpiringPoA
  alter column Task bigint
  
  alter table [dbo].[Sungero_Docflow_ExpiringPoA] add unique nonclustered ([EDoc] asc)
end

-- Удалить все старые прикладные функции.
declare @applied_proc varchar(max)

select @applied_proc = COALESCE(@applied_proc + ',' + [name], [name]) 
from sys.procedures 
where type = 'P' and name in ('Sungero_DocRegister_GetNextNumber', 'Sungero_Company_GetAllVisibleRecipients', 'Sungero_Company_GetHeadRecipientsByEmployee')

if @applied_proc is not null
  exec ('drop procedure ' + @applied_proc)