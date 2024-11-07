-- Заполнить свойство Рамочный для старых договоров.
update Sungero_Content_EDoc
set FrameContract_Contrac_Sungero = 0
where FrameContract_Contrac_Sungero is null
and Discriminator = 'f37c7e63-b134-4446-9b5b-f8811f6c9666'