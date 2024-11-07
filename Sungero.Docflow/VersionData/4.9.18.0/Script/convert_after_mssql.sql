IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_AccCParty')
BEGIN
  DROP INDEX idx_EDoc_AccCParty ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_BusinessUnit')
BEGIN
  DROP INDEX idx_EDoc_BusinessUnit ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_Department')
BEGIN
  DROP INDEX idx_EDoc_Department ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_Counterparty')
BEGIN
  DROP INDEX idx_EDoc_Counterparty ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_DocRegister')
BEGIN
  DROP INDEX idx_EDoc_DocRegister ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_DocumentKind')
BEGIN
  DROP INDEX idx_EDoc_DocumentKind ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_InCorr')
BEGIN
  DROP INDEX idx_EDoc_InCorr ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_RespEmpl')
BEGIN
  DROP INDEX idx_EDoc_RespEmpl ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_RegState')
BEGIN
  DROP INDEX idx_EDoc_RegState ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_Discr_DocDate_LifeCycleState_IntApprState_SecureObject')
BEGIN
  DROP INDEX idx_EDoc_Discr_DocDate_LifeCycleState_IntApprState_SecureObject ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_Discr_DocDate_RegState_DocKind_SecureObject')
BEGIN
  DROP INDEX idx_EDoc_Discr_DocDate_RegState_DocKind_SecureObject ON Sungero_Content_EDoc  
END

IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_Content_EDoc') and name ='idx_EDoc_Discriminator_DocumentDate_RegState_SecureObject')
BEGIN
  DROP INDEX idx_EDoc_Discriminator_DocumentDate_RegState_SecureObject ON Sungero_Content_EDoc  
END