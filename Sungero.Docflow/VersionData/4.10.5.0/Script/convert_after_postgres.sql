-- Название поля Extension написано в кавычках, потому что иначе возникает путаница с одноимённым служебным словом и скрипт не работает.
update sungero_content_associatedapp
set openbydefaultforreading = true
where "extension" in ('fb2', 'rar', '7z', 'zip', 'cdr', 'bmp', 'swf', 'flv', 'wmv', 'mp3', 'avi', 'djvu', 'gif', 'ico', 'jpeg', 'jpg', 'png', 'tiff', 'tif', 'hlp')