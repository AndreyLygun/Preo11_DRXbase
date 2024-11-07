if exists (select 1
       from [Sungero_Docflow_DocumentType]
       where [DoctypeGuid] = '09584896-81e2-4c83-8f6c-70eb8321e1d0' and [Name] = 'Простой документ' )
  update [Sungero_MobApps_MobAppSetting]
  set [OfflTrdDesc] = 'Если флажок установлен, данные в приложение могут загружаться медленнее'
  where [OfflTrdDesc] is null
else
  update [Sungero_MobApps_MobAppSetting]
  set [OfflTrdDesc] = 'If the checkbox is selected, app data might load more slowly'
  where [OfflTrdDesc] is null

update [Sungero_MobApps_MobAppSetting]
  set [OfflineThreads] = 'false'
  where [OfflineThreads] is null