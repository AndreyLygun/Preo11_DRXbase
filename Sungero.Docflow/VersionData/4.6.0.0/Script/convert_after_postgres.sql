do $$
begin
  if exists(select 1 from information_schema.tables where table_schema = 'public' and table_name = 'sungero_docflow_params') then 
    if exists(select 1 from public.sungero_docflow_params where Key = 'GrantRightsMode') then
      delete from public.sungero_docflow_params where Key = 'GrantRightsMode';
    end if;
  end if;
end $$