-- С версии 4.11.14.0 отступы для отметки о поступлении должны быть больше нуля.
update sungero_docflow_personsetting
set rightindent = 0.01
where rightindent = 0;
update sungero_docflow_personsetting
set bottomindent = 0.01
where bottomindent = 0;