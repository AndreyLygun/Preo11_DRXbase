do $$
begin

update sungero_exch_exchdocinfo
set deliverystatus = 'Processed' where deliverystatus = 'Sent';

update sungero_exch_exchdocinfo
set buyerdlvstatus = 'NotRequired' where buyerdlvstatus is null;

end $$