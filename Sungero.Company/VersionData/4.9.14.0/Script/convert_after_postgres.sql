-- У сотрудников, чьи карточки закрыты на момент обновления, проставляется дата прекращения системного замещения.
-- Дискриминатор справочника Employee (b7905516-2be5-4931-961c-cb38d5677565).
DO $$
DECLARE minDate date = date (NOW()) + integer '1800';
BEGIN
UPDATE Sungero_Core_Substitution as s
SET EndDate = CASE 
                WHEN date (CASE WHEN d.HistoryDate is null THEN NOW() ELSE d.HistoryDate END) + integer '1980' < minDate
                THEN minDate
                ELSE date (CASE WHEN d.HistoryDate is null THEN NOW() ELSE d.HistoryDate END) + integer '1980'
              END
FROM Sungero_Core_Recipient r
LEFT JOIN (SELECT h.EntityId as Id, max(h.HistoryDate) as HistoryDate 
           FROM Sungero_Core_DatabookHistory h
		       WHERE h.Action = 'Update'
		         AND h.EntityType = 'b7905516-2be5-4931-961c-cb38d5677565'
		       GROUP BY h.EntityId) d
  ON d.Id = r.Id
WHERE s.IsSystem = True
  AND s.EndDate is null
  AND r.Status = 'Closed'
  AND s.SubstUser = r.Id;
  
END $$;