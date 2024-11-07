--Обновить Url для открытия МЧД на сайте ФНС.
if exists(select 1 from information_schema.tables where table_schema = 'dbo' and table_name = 'Sungero_Docflow_Params') 
update [dbo].[Sungero_Docflow_Params]
set [Value] = 'https://m4d.nalog.gov.ru/EMCHD/get-info?guid={0}&innP={1}&innR={2}'
where [Key] = 'SearchFPoAInFtsRegistryTemplate'
  and [Value] = 'https://m4d-cprr-it.gnivc.ru/search-full?poaNumber={0}&issuerInn={1}&representativeInn={2}'

update Sungero_Content_EDoc
set PowersType_Docflow_Sungero = 'FreeForm' 
where PowersType_Docflow_Sungero is null
  and Discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B' -- Эл. доверенность
