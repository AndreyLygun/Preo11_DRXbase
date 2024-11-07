-- Заполнить у старых эл. доверенностей формат по умолчанию - 002, если есть публичное тело.
update doc
set doc.FormatVersion_Docflow_Sungero = 'Version002'
from Sungero_Content_EDoc doc
where doc.Discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B'
  and doc.FormatVersion_Docflow_Sungero is null
  and (select top 1 v.PublicBody_Size
       from Sungero_Content_EDocVersion v
       where v.EDoc = doc.Id 
       order by Id desc) > 0;
