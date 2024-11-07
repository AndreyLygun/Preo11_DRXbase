-- Название поля Extension написано в кавычках, потому что иначе возникает путаница с одноимённым служебным словом и скрипт не работает.
update Sungero_Content_AssociatedApp
set OpenByDefaultForReading = 1
where "Extension" in ('fb2', 'rar', '7z', 'zip', 'cdr', 'bmp', 'swf', 'flv', 'wmv', 'mp3', 'avi', 'djvu', 'gif', 'ico', 'jpeg', 'jpg', 'png', 'tiff', 'tif', 'hlp')