DO $$
begin
-- Обновление неправильно проставленного при конвертации идентификатора таблицы.
update sungero_docflow_representativs 
  set discriminator='185BC813-FA53-4C29-A07C-6C04397DFF41' -- Дочерняя коллекция Представители
  where discriminator != '185BC813-FA53-4C29-A07C-6C04397DFF41';

end $$;