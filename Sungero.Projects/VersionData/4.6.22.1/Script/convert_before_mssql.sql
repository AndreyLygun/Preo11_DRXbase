create table #leading_project_temp
(
 	Id bigint,
	LeadingProjectId bigint
)

if (exists (select * from INFORMATION_SCHEMA.TABLES 
                     where TABLE_NAME = 'Sungero_Docflow_Project'))
begin
  insert into #leading_project_temp (Id, LeadingProjectId) select Id, LeadingProject_Project_Sungero from [dbo].[Sungero_Docflow_Project]
end;

if (exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Project_ProjectTeamMem') and
	exists (select * from INFORMATION_SCHEMA.COLUMNS where table_name = 'Sungero_Project_ProjectTeamMem' and column_name = 'Project'))
begin
	exec sp_rename 'dbo.Sungero_Project_ProjectTeamMem.Project' , 'ProjectCore', 'COLUMN';
end;

if (exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Project_ProjectClassif') and
	exists (select * from INFORMATION_SCHEMA.COLUMNS where table_name = 'Sungero_Project_ProjectClassif' and column_name = 'Project'))
begin
	exec sp_rename 'dbo.Sungero_Project_ProjectClassif.Project' , 'ProjectCore', 'COLUMN';
end;