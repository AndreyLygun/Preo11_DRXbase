DO $$
begin

update sungero_content_edoc
set delegationtype_docflow_sungero = 'NoDelegation'  
where delegationtype_docflow_sungero is null
  and discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B'; -- Эл. доверенность.
  
update sungero_content_edoc
set isdelegated_docflow_sungero = false
where isdelegated_docflow_sungero is null
  and discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B'; -- Эл. доверенность.
  
update sungero_content_edoc
set isnotarized_docflow_sungero = false
where isnotarized_docflow_sungero is null
  and discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B'; -- Эл. доверенность.

end $$;

-- Копирование единственного представителя из таблицы sungero_content_edoc в sungero_docflow_representativs.
do $$
declare
	incrementRange int := 1;
	newId bigint;
	edocId bigint;
	discrim uuid := '185BC813-FA53-4C29-A07C-6C04397DFF41'; -- Дочерняя коллекция Представители
	agenttype citext;
	issuedtoparty bigint;
	representative bigint;
begin 

  for edocId, agenttype, issuedtoparty, representative in
    select id,
	    case when agenttype_docflow_sungero='Employee' then 'Person' else agenttype_docflow_sungero end as agentType,
	    issuedtoparty_docflow_sungero, representative_docflow_sungero
    from sungero_content_edoc
	where discriminator in ('104472DB-B71B-42A8-BCA5-581A08D1CA7B', 'BE859F9B-7A04-4F07-82BC-441352BCE627') -- Эл. доверенность; доверенность.
		and ismanyreprsnts_docflow_sungero is null
  loop
    if not exists (select 1 from sungero_docflow_representativs where edoc = edocId) then
      newId = sungero_system_GetNewId('sungero_docflow_representativs', incrementRange);
      insert into sungero_docflow_representativs(id,discriminator,edoc,agenttype,issuedto,agent)
      values(newId, discrim, edocId, agenttype, issuedtoparty, representative);
	  
	  update public.sungero_content_edoc set ismanyreprsnts_docflow_sungero=false
	  	where id=edocId;
    end if;
  end loop;

end $$;
-- Конец копирования единственного представителя.