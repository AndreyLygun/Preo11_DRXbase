-- Добавление записей справочника "Вид носителя документа".
declare @IncrementRange int = 1
declare @newId bigint
declare @mediumType varchar(36) = 'e84eb4d1-e972-4cdd-b573-12f203fa159f'
declare @entitySid varchar(36)
declare @mediumTypeName varchar(500)
declare @status varchar(500)

declare @mediumTypes table(sid varchar(36), name varchar(500), status varchar(500))
-- Имена в одной локали осознанно, потому что актуализируются под локаль в инициализации.
insert into @mediumTypes
select
'569AC075-D17A-4C64-B84B-4496E8CDDBAE', 'Электронная', 'Active'
union select
'27E9EBCA-35F5-4202-9C15-1CBE2C0EF4E9', 'Бумажная', 'Active'
union select
'4BEBBB70-B554-4AE9-AB21-B712EEDECD1F', 'Гибридная', 'Closed'

declare cur cursor for 
select *
from @mediumTypes

open cur
fetch next from cur into @entitySid, @mediumTypeName, @status
while @@fetch_status = 0 
begin  
  if not exists (select 1
                 from dbo.Sungero_Docflow_MediumType
                 where Sid = @entitySid)
  begin
    exec Sungero_System_GetNewId 'Sungero_Docflow_MediumType', @newId output, @IncrementRange

    insert into dbo.Sungero_Docflow_MediumType
    values(@newId, @mediumType, 1, null, @status, @mediumTypeName, null, @entitySid)
  end

  fetch next from cur into @entitySid, @mediumTypeName, @status
end
close cur
deallocate cur

-- Заполнить свойство Medium записью "Электронная" для электронных доверенностей, заявлений на отзыв, соглашений об аннулировании, формализованных документов,
-- а также для документов, ходивших через сервис обмена.
declare @electronicMediumType bigint

select @electronicMediumType = Id
from dbo.Sungero_Docflow_MediumType
where Sid = '569AC075-D17A-4C64-B84B-4496E8CDDBAE'

update edoc
set edoc.Medium_Docflow_Sungero = @electronicMediumType
from dbo.Sungero_Content_EDoc as edoc
where edoc.Medium_Docflow_Sungero is NULL
  and (edoc.IsFormalized_Docflow_Sungero = 1
      or edoc.Discriminator in ('104472db-b71b-42a8-bca5-581a08d1ca7b', -- МЧД
                                '298d33ab-490e-47ff-af80-d6147194e1f7', -- Отзыв
                                '4c57f798-1547-4de0-b240-d9d97901df5f', -- СоА
                                'cf8357c3-8266-490d-b75e-0bd3e46b1ae8') -- Вх. документ эл. обмена
      or exists (select 1
                 from dbo.Sungero_Exch_Exchdocinfo as ex
                 where ex.document = edoc.id))