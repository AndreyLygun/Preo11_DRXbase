--Обновить Url для открытия МЧД на сайте ФНС.
do $$
begin
  if exists(select 1 from information_schema.tables where table_schema = 'public' and table_name = 'sungero_docflow_params') then 
    update sungero_docflow_params
    set value = 'https://m4d.nalog.gov.ru/EMCHD/get-info?guid={0}&innP={1}&innR={2}'
    where key = 'SearchFPoAInFtsRegistryTemplate'
      and value = 'https://m4d-cprr-it.gnivc.ru/search-full?poaNumber={0}&issuerInn={1}&representativeInn={2}';
  end if;
 
update sungero_content_edoc
set powerstype_docflow_sungero = 'FreeForm'  
where powerstype_docflow_sungero is null
  and discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B'; -- Эл. доверенность.

end $$