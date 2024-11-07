do $$
begin

-- Заполнить свойство Рамочный для старых договоров.
update sungero_content_edoc
set framecontract_contrac_sungero = false
where framecontract_contrac_sungero is null
and discriminator = 'f37c7e63-b134-4446-9b5b-f8811f6c9666';

end $$;