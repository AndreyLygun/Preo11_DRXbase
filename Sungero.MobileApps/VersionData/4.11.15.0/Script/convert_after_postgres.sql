do $$
begin
if exists (select 1
       from sungero_docflow_documenttype
       where doctypeguid = '09584896-81e2-4c83-8f6c-70eb8321e1d0' and name = 'Простой документ' )
then
  update sungero_mobapps_mobappsetting
  set offltrddesc = 'Если флажок установлен, данные могут загружаться в приложение дольше';
else
  update sungero_mobapps_mobappsetting
  set offltrddesc = 'If the checkbox is selected, loading data to the app might take more time';
end if;

end $$;