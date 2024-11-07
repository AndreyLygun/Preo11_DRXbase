-- У сотрудников, чьи карточки закрыты на момент обновления, проставляется дата прекращения системного замещения.
-- Дискриминатор справочника Employee (b7905516-2be5-4931-961c-cb38d5677565).
DECLARE @minDate DateTime = DATEADD(day, 1800, GETDATE());

UPDATE Sungero_Core_Substitution
SET EndDate = CASE 
                WHEN DATEADD(day, 1980, isnull(d.HistoryDate, GETDATE())) < @minDate 
				        THEN @minDate 
				        ELSE DATEADD(day, 1980, isnull(d.HistoryDate, GETDATE())) 
			        END
FROM Sungero_Core_Substitution s
JOIN Sungero_Core_Recipient r
  ON s.SubstUser = r.Id
LEFT JOIN (SELECT h.EntityId as Id, max(h.HistoryDate) as HistoryDate 
           FROM Sungero_Core_DatabookHistory h
		       WHERE h.Action = 'Update'
		         AND h.EntityType = 'b7905516-2be5-4931-961c-cb38d5677565'
		       GROUP BY h.EntityId) d
  ON d.Id = r.Id
WHERE s.IsSystem = 1
  AND s.EndDate is null
  AND r.Status = 'Closed';