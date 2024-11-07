insert into Sungero_System_AutotextStatistics (UserId, Text, PropertyId, EntityId, LastUpdate, Count)
  select
    UserId,
    Text,
    'd74c0b99-b7ca-480f-bb05-0fccf92a47b6',
    '50e39d87-4fc6-4847-8bad-20847b9ba020',
    MAX(LastUpdate),
    SUM(Count)
  from Sungero_System_AutotextStatistics
  where (EntityId = 'e2dd5bf3-54c8-4846-b158-9c42d09fbc33'
     or EntityId = '69ac657a-0e74-46be-acba-f6bbbbd2bc73')
    and not exists (select 1 from Sungero_System_AutotextStatistics where EntityId = '50e39d87-4fc6-4847-8bad-20847b9ba020')
  group by Text, UserId