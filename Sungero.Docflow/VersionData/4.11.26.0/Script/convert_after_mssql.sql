-- Обновление неправильно проставленного при конвертации идентификатора таблицы.
update dbo.Sungero_Docflow_Representativs 
  set Discriminator='185BC813-FA53-4C29-A07C-6C04397DFF41' -- Дочерняя коллекция Представители
  where Discriminator != '185BC813-FA53-4C29-A07C-6C04397DFF41'