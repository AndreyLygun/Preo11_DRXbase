IF EXISTS(select 1 from sys.indexes where object_id = (select object_id from sys.objects where name ='Sungero_WF_Task') and name ='idx_Assignment_Task_Discriminator_Status')
BEGIN
  DROP INDEX idx_Assignment_Task_Discriminator_Status ON Sungero_WF_Task  
END

IF NOT EXISTS(select 1 from sys.indexes where name = 'idx_Assignment_Task_Discriminator_Status_4_8_0' and object_id = (select object_id from sys.objects where name ='Sungero_WF_Task'))
BEGIN
  CREATE NONCLUSTERED INDEX [idx_Assignment_Task_Discriminator_Status_4_8_0] ON [dbo].[Sungero_WF_Task]
  (
    [Discriminator] ASC,
    [Status] ASC
  )
  INCLUDE
  (
    [DeadlineTAI_RecMan_Sungero],
    [IsCompound_RecMan_Sungero],
    [ActualDate_RecMan_Sungero],
    [MainTask],
    [AssigneeTAI_RecMan_Sungero],
    [Author],
    [StartedBy],
    [ActionItemTAI_RecMan_Sungero],
    [ExecutionState_RecMan_Sungero],
    [ActionItemType_RecMan_Sungero]
  )
END