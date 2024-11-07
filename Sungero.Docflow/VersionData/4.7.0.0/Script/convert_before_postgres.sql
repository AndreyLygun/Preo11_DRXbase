do $$
DECLARE drop_applied_functions text;

begin

-- Сконвертировать поля с типом int таблицы Sungero_DocRegister_CurrentNumbers в bigint.
if exists (select column_name
           from information_schema.columns
           where table_name='sungero_docregister_currentnumbers' 
             and column_name in ('docregisterid', 'leaddocument', 'department', 'bunit') 
             and DATA_TYPE = 'integer') then
			 
  alter table sungero_docregister_currentnumbers
  alter column docregisterid type bigint,
  alter column leaddocument type bigint,
  alter column department type bigint,
  alter column bunit type bigint;
end if;

-- Сконвертировать поля с типом int таблицы Sungero_Contrac_ExpiringContracts в bigint.
if exists (select column_name
           from information_schema.columns
           where table_name='sungero_contrac_expiringcontracts' 
             and column_name in ('edoc', 'task') 
             and DATA_TYPE = 'integer') then
			 
  alter table sungero_contrac_expiringcontracts
  alter column edoc type bigint,
  alter column task type bigint;
end if;

-- Сконвертировать поля с типом int таблицы Sungero_Docflow_ExpiringPoA в bigint.
if exists (select column_name
           from information_schema.columns
           where table_name='sungero_docflow_expiringpoa'
             and column_name in ('edoc', 'task')
             and DATA_TYPE = 'integer') then
			 
  alter table sungero_docflow_expiringpoa
  alter column edoc type bigint,
  alter column task type bigint;
end if;

-- Удалить все старые прикладные функции, так как изменилась сигнатура и replace их не заменит.
select into drop_applied_functions
  string_agg(format('DROP FUNCTION IF EXISTS %s;', oid::regprocedure), E'\n')
from pg_proc
where proname ~* 'sungero_company_getallvisiblerecipients|sungero_company_getheadrecipientsbyemployee|sungero_docregister_getnextnumber|sungero_docregister_setcurrentnumber';

if drop_applied_functions is not null then
  execute drop_applied_functions;
end if;

end $$