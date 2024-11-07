update
  dbo.sungero_docflow_approvalstage
set
  addregdetails = 0
where
  discriminator = '77fe4545-9220-4cde-9cf7-a254d28b3ba5'
  and addregdetails is null