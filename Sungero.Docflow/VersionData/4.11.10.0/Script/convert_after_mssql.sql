-- Заполнить свойство "На несколько исходящих" значением false для входящих писем.
update dbo.Sungero_Content_EDoc
set IsManyOutgResp_Docflow_Sungero = 0
where Discriminator = '8dd00491-8fd0-4a7a-9cf3-8b6dc2e6455d'
and IsManyOutgResp_Docflow_Sungero is null

-- Заполнить свойство "На несколько входящих" значением false для исходящих писем.
update dbo.Sungero_Content_EDoc
set IsManyIncResp_Docflow_Sungero = 0
where Discriminator = 'd1d2a452-7732-4ba8-b199-0a4dc78898ac'
and IsManyIncResp_Docflow_Sungero is null

-- Очистить заполнение свойств для типа связи "Ответное письмо".
update Sungero_Core_RelationTypeMa
set TargetProperty = null
from Sungero_Core_Relationtype r, Sungero_Core_RelationTypeMa m
where r.Name = 'Response' and r.Id = m.RelationType

-- Обновить коллекции "В ответ на" для входящих и исходящих писем.
declare @incrementRange int = 1
declare @newId bigint
declare @edocId bigint
declare @documentId bigint
declare @inResponseToOutg table(edoc bigint, document bigint)
declare @inResponseToInc table(edoc bigint, document bigint)

-- Добавить записи в коллекцию "В ответ на" для входящих писем.
insert into @inResponseToOutg
  select Id, InRespTo_Docflow_Sungero
  from dbo.Sungero_Content_EDoc
  where Discriminator = '8dd00491-8fd0-4a7a-9cf3-8b6dc2e6455d'
  and InRespTo_Docflow_Sungero is not null
  and IsManyOutgResp_Docflow_Sungero = 0
  
declare cur cursor for 
select *
from @inResponseToOutg

open cur

fetch next from cur into @edocId, @documentId
while @@fetch_status = 0 
begin
  if not exists (select 1 from dbo.Sungero_Docflow_InRespToOutg where EDoc = @edocId and Document = @documentId)
  begin
    exec Sungero_System_GetNewId 'Sungero_Docflow_InRespToOutg', @newId output, @incrementRange
    insert into dbo.Sungero_Docflow_InRespToOutg
    values(@newId, '097ec4e3-bae6-45cf-8760-b5b88befbd0a', @edocId, @documentId)
  end
  
  fetch next from cur into @edocId, @documentId
end

close cur
deallocate cur

-- Добавить записи в коллекцию "В ответ на" для исходящих писем.
insert into @inResponseToInc
  select Id, OutRespTo_Docflow_Sungero
  from dbo.Sungero_Content_EDoc
  where Discriminator = 'd1d2a452-7732-4ba8-b199-0a4dc78898ac'
  and OutRespTo_Docflow_Sungero is not null
  and IsManyIncResp_Docflow_Sungero = 0
  
declare cur cursor for 
select *
from @inResponseToInc

open cur

fetch next from cur into @edocId, @documentId
while @@fetch_status = 0 
begin
  if not exists (select 1 from dbo.Sungero_Docflow_InRespToInc where EDoc = @edocId and Document = @documentId)
  begin
    exec Sungero_System_GetNewId 'Sungero_Docflow_InRespToInc', @newId output, @incrementRange
    insert into dbo.Sungero_Docflow_InRespToInc
    values(@newId, '2a74766a-7795-47ec-abba-eb5bd01f6f0b', @edocId, @documentId)
  end
  
  fetch next from cur into @edocId, @documentId
end

close cur
deallocate cur