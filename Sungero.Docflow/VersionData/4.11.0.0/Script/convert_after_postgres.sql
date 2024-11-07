-- Добавление записей справочника "Вид носителя документа".
do $$
declare 
  IncrementRange int := 1;
  newId bigint;
  mediumType uuid := 'e84eb4d1-e972-4cdd-b573-12f203fa159f';
  entitySid varchar(36);
  mediumTypeName varchar(500);
  entityStatus varchar(500);
begin
  create temp table mediumTypes(sid uuid, name varchar(500), status varchar(500));

  -- Имена в одной локали осознанно, потому что актуализируются под локаль в инициализации.
  insert into mediumTypes(sid, name, status)
  values
  ('569AC075-D17A-4C64-B84B-4496E8CDDBAE', 'Электронная', 'Active'),
  ('27E9EBCA-35F5-4202-9C15-1CBE2C0EF4E9', 'Бумажная', 'Active'),
  ('4BEBBB70-B554-4AE9-AB21-B712EEDECD1F', 'Гибридная', 'Closed');

  for entitySid, mediumTypeName, entityStatus in 
    select mt.sid, mt.name, mt.status
    from mediumTypes as mt
  loop
    if not exists (select 1 from Sungero_Docflow_MediumType where Sid = entitySid) then
      newId = Sungero_System_GetNewId('Sungero_Docflow_MediumType', IncrementRange);

      insert into Sungero_Docflow_MediumType
      values(newId, mediumType, 1, null, entityStatus, mediumTypeName, null, entitySid);
    end if;
  end loop;
end $$;

-- Заполнить свойство Medium записью "Электронная" для электронных доверенностей, заявлений на отзыв, соглашений об аннулировании, формализованных документов,
-- а также для документов, ходивших через сервис обмена.
do $$
declare
    electronicmediumtype bigint;
begin
    select id into electronicmediumtype
    from sungero_docflow_mediumtype
    where sid = '569ac075-d17a-4c64-b84b-4496e8cddbae';

    update sungero_content_edoc as edoc
    set medium_docflow_sungero = electronicmediumtype
    where edoc.medium_docflow_sungero is null
      and (edoc.isformalized_docflow_sungero = true
          or edoc.discriminator in ('104472db-b71b-42a8-bca5-581a08d1ca7b', -- МЧД
                                    '298d33ab-490e-47ff-af80-d6147194e1f7', -- Отзыв
                                    '4c57f798-1547-4de0-b240-d9d97901df5f', -- СоА
                                    'cf8357c3-8266-490d-b75e-0bd3e46b1ae8') -- Вх. документ эл. обмена
          or exists (select 1
                     from sungero_exch_exchdocinfo as ex
                     where ex.document = edoc.id));
end $$;