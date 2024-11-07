update [dbo].[Sungero_Docflow_Project]
  set LeadingProject_Project_Sungero = tmp.LeadingProjectId
from [dbo].[Sungero_Docflow_Project] as project
join #leading_project_temp as tmp on tmp.Id = project.Id