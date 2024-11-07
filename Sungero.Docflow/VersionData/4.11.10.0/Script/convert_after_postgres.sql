do $$
begin
-- Заполнить свойство "На несколько исходящих" значением false для входящих писем.
update sungero_content_edoc
set ismanyoutgresp_docflow_sungero = false
where discriminator = '8dd00491-8fd0-4a7a-9cf3-8b6dc2e6455d'
and ismanyoutgresp_docflow_sungero is null;

-- Заполнить свойство "На несколько входящих" значением false для исходящих писем.
update sungero_content_edoc
set ismanyincresp_docflow_sungero = false
where discriminator = 'd1d2a452-7732-4ba8-b199-0a4dc78898ac'
and ismanyincresp_docflow_sungero is null;

-- Очистить заполнение свойств для типа связи "Ответное письмо".
update sungero_core_relationtypema as m
set targetproperty = null
from sungero_core_relationtype as r
where r.name = 'Response' and r.id = m.relationtype;
end $$;

-- Обновить коллекции "В ответ на" для входящих и исходящих писем.
do $$
declare
	incrementRange int := 1;
	newId bigint;
	edocId bigint;
	documentId bigint;
	
begin
-- Добавить записи в коллекцию "В ответ на" для входящих писем.
  create temp table inResponseToOutg(edoc bigint, document bigint);
  insert into inResponseToOutg (edoc, document)
    select id, inrespto_docflow_sungero
    from sungero_content_edoc
    where discriminator = '8dd00491-8fd0-4a7a-9cf3-8b6dc2e6455d'
    and inrespto_docflow_sungero is not null
    and ismanyoutgresp_docflow_sungero = false;

  for edocId, documentId in
    select edoc, document
    from inResponseToOutg
  loop
    if not exists (select 1 from sungero_docflow_inresptooutg where edoc = edocId and document = documentId) then
      newId = sungero_system_GetNewId('sungero_docflow_inresptooutg', incrementRange);
      insert into sungero_docflow_inresptooutg
      values(newId, '097ec4e3-bae6-45cf-8760-b5b88befbd0a', edocId, documentId);
    end if;
  end loop;

-- Добавить записи в коллекцию "В ответ на" для исходящих писем.
  create temp table inResponseToInc(edoc bigint, document bigint);
  insert into inResponseToInc (edoc, document)
    select id, outrespto_docflow_sungero
    from sungero_content_edoc
    where discriminator = 'd1d2a452-7732-4ba8-b199-0a4dc78898ac'
    and outrespto_docflow_sungero is not null
    and ismanyincresp_docflow_sungero = false;

  for edocId, documentId in
    select edoc, document
    from inResponseToInc
  loop
    if not exists (select 1 from sungero_docflow_inresptoinc where edoc = edocId and document = documentId) then
      newId = sungero_system_GetNewId('sungero_docflow_inresptoinc', incrementRange);
      insert into sungero_docflow_inresptoinc
      values(newId, '2a74766a-7795-47ec-abba-eb5bd01f6f0b', edocId, documentId);
    end if;
  end loop;
end $$;