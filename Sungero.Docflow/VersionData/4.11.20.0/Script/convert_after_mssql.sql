update Sungero_Content_EDoc
set DelegationType_Docflow_Sungero = 'NoDelegation' 
where DelegationType_Docflow_Sungero is null
  and Discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B' -- Эл. доверенность
  
update Sungero_Content_EDoc
set IsDelegated_Docflow_Sungero = 0
where IsDelegated_Docflow_Sungero is null
  and Discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B' -- Эл. доверенность
  
update Sungero_Content_EDoc
set IsNotarized_Docflow_Sungero = 0
where IsNotarized_Docflow_Sungero is null
  and Discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B' -- Эл. доверенность
  
-- Копирование единственного представителя из таблицы sungero_content_edoc в sungero_docflow_representativs.
declare @incrementRange int = 1
declare @newId bigint
declare @edocId bigint
declare @discriminator varchar(50) = '185BC813-FA53-4C29-A07C-6C04397DFF41' -- Дочерняя коллекция Представители
declare @agenttype varchar(50)
declare @issuedtoparty bigint
declare @representative bigint 
declare @tempRepresentative table(id bigint, agenttype_docflow_sungero varchar(50), issuedtoparty_docflow_sungero bigint, representative_docflow_sungero bigint)

insert into @tempRepresentative
  select Id,
    case when AgentType_Docflow_Sungero='Employee' then 'Person' else AgentType_Docflow_Sungero end as agentType,
	  IssuedToParty_Docflow_Sungero, Representative_Docflow_Sungero
    from dbo.Sungero_Content_EDoc
	where Discriminator in ('104472DB-B71B-42A8-BCA5-581A08D1CA7B','BE859F9B-7A04-4F07-82BC-441352BCE627') -- Эл. доверенность; доверенность.
		and IsManyReprsnts_Docflow_Sungero is null
  
declare cur cursor for 
select * from @tempRepresentative

open cur

fetch next from cur into @edocId, @agenttype, @issuedtoparty, @representative
while @@fetch_status = 0 
begin
  if not exists (select 1 from dbo.Sungero_Docflow_Representativs where edoc = @edocId)
  begin
    exec Sungero_System_GetNewId 'sungero_docflow_representativs', @newId output, @incrementRange
    insert into dbo.Sungero_Docflow_Representativs(Id, Discriminator, EDoc, AgentType, IssuedTo, Agent)
    values(@newId, @discriminator, @edocId, @agenttype, @issuedtoparty, @representative);

	update dbo.Sungero_Content_EDoc set IsManyReprsnts_Docflow_Sungero=0 where Id=@edocId;
  end
  
  fetch next from cur into @edocId, @agenttype, @issuedtoparty, @representative
end

close cur
deallocate cur
-- Конец копирования единственного представителя.