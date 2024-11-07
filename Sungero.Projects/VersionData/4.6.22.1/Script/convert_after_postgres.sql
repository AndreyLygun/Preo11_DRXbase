DO $$
begin

if exists(select * from information_schema.tables where table_name = 'leading_project_temp')
then
	update sungero_docflow_project as project
	  set leadingproject_project_sungero = tmp.leadingprojectid
	from leading_project_temp as tmp
	where tmp.Id = project.Id;

  drop table leading_project_temp;
end if;

end $$;