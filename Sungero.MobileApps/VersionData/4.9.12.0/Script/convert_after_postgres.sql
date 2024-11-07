do $$
begin
if exists (select 1
       from sungero_docflow_documenttype
       where doctypeguid = '09584896-81e2-4c83-8f6c-70eb8321e1d0' and name = 'Простой документ' )
then
  update sungero_mobapps_mobappsetting
  set isvisfldrdesc = 'Если флажок снят, в мобильном приложении отображаются все папки сотрудника'
  where isvisfldrdesc is null;
else
  update sungero_mobapps_mobappsetting
  set isvisfldrdesc = 'If the checkbox is cleared, all employee''s folders are displayed in the mobile app'
  where isvisfldrdesc is null;
end if;

update sungero_mobapps_mobappsetting
  set visblfldrlimit = 'true'
  where visblfldrlimit is null;
end $$;