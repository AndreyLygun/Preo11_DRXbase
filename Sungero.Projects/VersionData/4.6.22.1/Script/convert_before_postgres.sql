DO $$
begin

if exists(select * from information_schema.tables where table_name = 'leading_project_temp')
then
  drop table leading_project_temp;
end if;

create table leading_project_temp
(
	id bigint,
	leadingprojectid bigint
);

if (exists (select * from information_schema.tables where table_name = 'sungero_docflow_project'))
then
  insert into leading_project_temp (id, leadingprojectid)
  select id, leadingproject_project_sungero from sungero_docflow_project;
end if;


if (exists (select * from information_schema.tables where table_name = 'sungero_project_projectteammem') and
    exists (select * from information_schema.columns where table_name = 'sungero_project_projectteammem' and column_name='project'))
then
	alter table sungero_project_projectteammem 
	rename column project to projectcore;
end if;

if (exists (select * from information_schema.tables where table_name = 'sungero_project_projectclassif') and
    exists (select * from information_schema.columns where table_name = 'sungero_project_projectclassif' and column_name='project'))
then
	alter table sungero_project_projectclassif 
	rename column project to projectcore;
end if;

end $$;