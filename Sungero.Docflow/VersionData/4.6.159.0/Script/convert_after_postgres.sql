DO $$
begin

-- Заполнить у старых эл. доверенностей формат по умолчанию - 002, если есть публичное тело.
update Sungero_Content_EDoc doc
set FormatVersion_Docflow_Sungero = 'Version002'
where doc.Discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B'
  and doc.FormatVersion_Docflow_Sungero is null
  and (select v.PublicBody_Size
       from Sungero_Content_EDocVersion v
       where v.EDoc = doc.Id 
       order by Id desc
       limit 1) > 0;
       
end $$;