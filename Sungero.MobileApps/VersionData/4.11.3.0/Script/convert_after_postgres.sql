do $$
begin
if exists (select 1
       from sungero_docflow_documenttype
       where doctypeguid = '09584896-81e2-4c83-8f6c-70eb8321e1d0' and name = 'Простой документ' )
then
  update sungero_mobapps_mobappsetting
  set offltrddesc = 'Если флажок установлен, данные в приложение могут загружаться медленнее'
  where offltrddesc is null;
else
  update sungero_mobapps_mobappsetting
  set offltrddesc = 'If the checkbox is selected, app data might load more slowly'
  where offltrddesc is null;
end if;

update sungero_mobapps_mobappsetting
  set offlinethreads = 'false'
  where offlinethreads is null;
end $$;