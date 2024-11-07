if exists (select 1
       from [Sungero_Docflow_DocumentType]
       where [DoctypeGuid] = '09584896-81e2-4c83-8f6c-70eb8321e1d0' and [Name] = 'Простой документ' )
  update [Sungero_MobApps_MobAppSetting]
  set [IsVisFldrDesc] = 'Если флажок снят, в мобильном приложении отображаются все папки сотрудника'
  where [IsVisFldrDesc] is null
else
  update [Sungero_MobApps_MobAppSetting]
  set [IsVisFldrDesc] = 'If the checkbox is cleared, all employee''s folders are displayed in the mobile app'
  where [IsVisFldrDesc] is null

update [Sungero_MobApps_MobAppSetting]
  set [VisblFldrLimit] = 'true'
  where [VisblFldrLimit] is null