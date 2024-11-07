-- Заполнить свойство Надежность в организациях, соответствующих нашим организациям.
update Sungero_Parties_Counterparty
set Reliability = 'High'
where Reliability is null
and IsReadOnly = 1