do $$
begin

-- Заполнить свойство Надежность в организациях, соответствующих нашим организациям.
update sungero_parties_counterparty
set reliability = 'High'
where reliability is null
and isreadonly = true;

end $$;