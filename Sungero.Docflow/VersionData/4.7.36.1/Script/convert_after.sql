-- Заполнить настройки уведомлений об отзыве моих эл. доверенностей и эл. доверенностей моих подчиненных.
update Sungero_Docflow_PersonSetting
set MyRevPoaNotif = PofAttoNotif, SubRevPoaNotif = SubPofAttNotif
where MyRevPoaNotif is null and SubRevPoaNotif is null;

-- Заполнить у старых доверенностей тип представителя по умолчанию - "Сотрудник".
update Sungero_Content_EDoc
set AgentType_Docflow_Sungero = 'Employee'
where (Discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B' or Discriminator = 'BE859F9B-7A04-4F07-82BC-441352BCE627')
  and AgentType_Docflow_Sungero is null;

-- Прокинуть персону из "Кому выдана" (сотрудник) в новое поле "Кому выдана" (контрагент).
update Sungero_Content_EDoc
set IssuedToParty_Docflow_Sungero = 
  (select r.Person_Company_Sungero 
  from Sungero_Core_Recipient r
  where IssuedTo_Docflow_Sungero = r.id)
where (Discriminator = '104472DB-B71B-42A8-BCA5-581A08D1CA7B' or Discriminator = 'BE859F9B-7A04-4F07-82BC-441352BCE627')
  and IssuedToParty_Docflow_Sungero is null
  and AgentType_Docflow_Sungero = 'Employee';